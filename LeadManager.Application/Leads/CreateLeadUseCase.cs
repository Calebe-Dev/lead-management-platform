using LeadManager.Application.Abstractions;
using LeadManager.Domain.Leads;

namespace LeadManager.Application.Leads;

public sealed class CreateLeadUseCase
{
    private readonly ILeadRepository _leadRepository;
    private readonly ILeadHistoryRepository _leadHistoryRepository;
    private readonly ILeadAssignmentService _leadAssignmentService;
    private readonly ILeadListCache _leadListCache;
    private readonly ILeadScoringService _leadScoringService;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IAuditTrailRepository _auditTrailRepository;

    public CreateLeadUseCase(
        ILeadRepository leadRepository,
        ILeadHistoryRepository leadHistoryRepository,
        ILeadAssignmentService leadAssignmentService,
        ILeadListCache leadListCache,
        ILeadScoringService leadScoringService,
        IOutboxRepository outboxRepository,
        IAuditTrailRepository auditTrailRepository)
    {
        _leadRepository = leadRepository;
        _leadHistoryRepository = leadHistoryRepository;
        _leadAssignmentService = leadAssignmentService;
        _leadListCache = leadListCache;
        _leadScoringService = leadScoringService;
        _outboxRepository = outboxRepository;
        _auditTrailRepository = auditTrailRepository;
    }

    public async Task<LeadResponse> ExecuteAsync(CreateLeadCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var duplicate = await _leadRepository.FindDuplicateAsync(
            command.Email,
            command.Phone,
            command.Cnpj ?? string.Empty,
            cancellationToken);

        if (duplicate is not null)
        {
            throw new DuplicateLeadException(duplicate.Lead.Id, duplicate.MatchedFields);
        }

        var lead = Lead.Create(
            command.Name,
            command.Email,
            command.Phone,
            command.Company,
            command.JobTitle,
            command.Source,
            command.Region,
            command.LeadType ?? string.Empty,
            command.ProductInterest ?? string.Empty,
            command.Cnpj ?? string.Empty,
            command.CampaignId);

        var aiScore = await _leadScoringService.ScoreAsync(lead, cancellationToken);
        if (aiScore.HasValue)
        {
            lead.ApplyScore(aiScore.Value);
        }

        var assignee = await _leadAssignmentService.SelectAssigneeAsync(lead, cancellationToken);
        if (!string.IsNullOrWhiteSpace(assignee))
        {
            lead.AssignTo(assignee);
        }

        await _leadRepository.AddAsync(lead, cancellationToken);

        var historyEntries = new List<LeadHistoryEntry>
        {
            LeadHistoryEntry.Create(lead.Id, "Created", "lead", string.Empty, "created"),
            LeadHistoryEntry.Create(lead.Id, "StatusChanged", "status", string.Empty, lead.Status.ToString()),
            LeadHistoryEntry.Create(lead.Id, "ScoreChanged", "score", string.Empty, lead.Score.ToString()),
            LeadHistoryEntry.Create(lead.Id, "DataChanged", "region", string.Empty, lead.Region)
        };

        if (!string.IsNullOrWhiteSpace(lead.AssignedTo))
        {
            historyEntries.Add(LeadHistoryEntry.Create(lead.Id, "AssignmentChanged", "assigned_to", string.Empty, lead.AssignedTo));
        }

        await _leadHistoryRepository.AddRangeAsync(historyEntries, cancellationToken);
        await _leadListCache.InvalidateAsync(cancellationToken);
        await _auditTrailRepository.WriteInteractionAsync(
            new InteractionAuditRecord(
                lead.Id,
                "lead_created",
                $$"""
                {"source":"{{lead.Source}}","region":"{{lead.Region}}","assignedTo":"{{lead.AssignedTo}}"}
                """,
                DateTime.UtcNow),
            cancellationToken);
        await _outboxRepository.EnqueueAsync(
            "lead.created",
            $$"""
            {"leadId":"{{lead.Id}}","score":{{lead.Score}},"temperature":"{{lead.Temperature}}"}
            """,
            $"lead-created:{lead.Id}",
            cancellationToken);

        if (lead.Temperature == LeadTemperature.Hot)
        {
            await _outboxRepository.EnqueueAsync(
                "lead.hot",
                $$"""
                {"leadId":"{{lead.Id}}","assignedTo":"{{lead.AssignedTo}}","score":{{lead.Score}}}
                """,
                $"lead-hot:{lead.Id}",
                cancellationToken);
        }

        return lead.ToResponse();
    }
}
