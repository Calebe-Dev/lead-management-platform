using LeadManager.Application.Abstractions;
using LeadManager.Domain.Leads;

namespace LeadManager.Application.Leads;

public sealed class RecalculateLeadScoreUseCase
{
    private readonly ILeadRepository _leadRepository;
    private readonly ILeadHistoryRepository _leadHistoryRepository;
    private readonly ILeadListCache _leadListCache;
    private readonly ILeadScoringService _leadScoringService;
    private readonly IAuditTrailRepository _auditTrailRepository;
    private readonly IOutboxRepository _outboxRepository;

    public RecalculateLeadScoreUseCase(
        ILeadRepository leadRepository,
        ILeadHistoryRepository leadHistoryRepository,
        ILeadListCache leadListCache,
        ILeadScoringService leadScoringService,
        IAuditTrailRepository auditTrailRepository,
        IOutboxRepository outboxRepository)
    {
        _leadRepository = leadRepository;
        _leadHistoryRepository = leadHistoryRepository;
        _leadListCache = leadListCache;
        _leadScoringService = leadScoringService;
        _auditTrailRepository = auditTrailRepository;
        _outboxRepository = outboxRepository;
    }

    public async Task<LeadResponse?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Lead id is required.", nameof(id));
        }

        var lead = await _leadRepository.GetByIdAsync(id, cancellationToken);
        if (lead is null)
        {
            return null;
        }

        var previousScore = lead.Score;
        var previousTemperature = lead.Temperature;

        var aiScore = await _leadScoringService.ScoreAsync(lead, cancellationToken);
        if (aiScore.HasValue)
        {
            lead.ApplyScore(aiScore.Value);
        }
        else
        {
            lead.RecalculateScore();
        }

        await _leadRepository.UpdateAsync(lead, cancellationToken);

        var historyEntries = new List<LeadHistoryEntry>();
        if (previousScore != lead.Score)
        {
            historyEntries.Add(LeadHistoryEntry.Create(lead.Id, "ScoreChanged", "score", previousScore.ToString(), lead.Score.ToString()));
        }

        if (previousTemperature != lead.Temperature)
        {
            historyEntries.Add(LeadHistoryEntry.Create(
                lead.Id,
                "ScoreChanged",
                "temperature",
                previousTemperature.ToString(),
                lead.Temperature.ToString()));
        }

        if (historyEntries.Count > 0)
        {
            await _leadHistoryRepository.AddRangeAsync(historyEntries, cancellationToken);
            await _auditTrailRepository.WriteAiDecisionAsync(
                new AiDecisionRecord(
                    lead.Id,
                    aiScore.HasValue ? "llm" : "fallback",
                    aiScore.HasValue ? "external-score" : "rule-based-fallback",
                    $$"""
                    {"oldScore":{{previousScore}},"newScore":{{lead.Score}}}
                    """,
                    DateTime.UtcNow),
                cancellationToken);
            await _outboxRepository.EnqueueAsync(
                "lead.score.changed",
                $$"""
                {"leadId":"{{lead.Id}}","oldScore":{{previousScore}},"newScore":{{lead.Score}}}
                """,
                $"lead-score:{lead.Id}:{lead.Score}",
                cancellationToken);
        }

        await _leadListCache.InvalidateAsync(cancellationToken);

        return lead.ToResponse();
    }
}
