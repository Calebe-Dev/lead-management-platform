using LeadManager.Application.Abstractions;
using LeadManager.Domain.Leads;

namespace LeadManager.Application.Leads;

public sealed class CreateLeadUseCase
{
    private readonly ILeadRepository _leadRepository;
    private readonly ILeadHistoryRepository _leadHistoryRepository;
    private readonly ILeadAssignmentService _leadAssignmentService;
    private readonly ILeadListCache _leadListCache;

    public CreateLeadUseCase(
        ILeadRepository leadRepository,
        ILeadHistoryRepository leadHistoryRepository,
        ILeadAssignmentService leadAssignmentService,
        ILeadListCache leadListCache)
    {
        _leadRepository = leadRepository;
        _leadHistoryRepository = leadHistoryRepository;
        _leadAssignmentService = leadAssignmentService;
        _leadListCache = leadListCache;
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
            command.Cnpj ?? string.Empty);

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

        return lead.ToResponse();
    }
}
