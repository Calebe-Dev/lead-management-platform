using LeadManager.Application.Abstractions;
using LeadManager.Domain.Leads;

namespace LeadManager.Application.Leads;

public sealed class RecalculateLeadScoreUseCase
{
    private readonly ILeadRepository _leadRepository;
    private readonly ILeadHistoryRepository _leadHistoryRepository;
    private readonly ILeadListCache _leadListCache;

    public RecalculateLeadScoreUseCase(
        ILeadRepository leadRepository,
        ILeadHistoryRepository leadHistoryRepository,
        ILeadListCache leadListCache)
    {
        _leadRepository = leadRepository;
        _leadHistoryRepository = leadHistoryRepository;
        _leadListCache = leadListCache;
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

        lead.RecalculateScore();
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
        }
        await _leadListCache.InvalidateAsync(cancellationToken);

        return lead.ToResponse();
    }
}
