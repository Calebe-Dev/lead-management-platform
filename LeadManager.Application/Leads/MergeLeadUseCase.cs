using LeadManager.Application.Abstractions;
using LeadManager.Domain.Leads;

namespace LeadManager.Application.Leads;

public sealed class MergeLeadUseCase
{
    private readonly ILeadRepository _leadRepository;
    private readonly ILeadHistoryRepository _leadHistoryRepository;
    private readonly ILeadListCache _leadListCache;

    public MergeLeadUseCase(
        ILeadRepository leadRepository,
        ILeadHistoryRepository leadHistoryRepository,
        ILeadListCache leadListCache)
    {
        _leadRepository = leadRepository;
        _leadHistoryRepository = leadHistoryRepository;
        _leadListCache = leadListCache;
    }

    public async Task<LeadResponse?> ExecuteAsync(Guid targetLeadId, MergeLeadCommand command, CancellationToken cancellationToken = default)
    {
        if (targetLeadId == Guid.Empty)
        {
            throw new ArgumentException("Target lead id is required.", nameof(targetLeadId));
        }

        ArgumentNullException.ThrowIfNull(command);
        if (command.SourceLeadId == Guid.Empty)
        {
            throw new ArgumentException("Source lead id is required.", nameof(command.SourceLeadId));
        }

        if (targetLeadId == command.SourceLeadId)
        {
            throw new ArgumentException("Target and source lead must be different.");
        }

        var targetLead = await _leadRepository.GetByIdAsync(targetLeadId, cancellationToken);
        if (targetLead is null)
        {
            return null;
        }

        var sourceLead = await _leadRepository.GetByIdAsync(command.SourceLeadId, cancellationToken);
        if (sourceLead is null)
        {
            throw new InvalidOperationException($"Source lead '{command.SourceLeadId}' was not found.");
        }

        var previousAssignedTo = targetLead.AssignedTo;
        var previousCampaignId = targetLead.CampaignId;
        var previousScore = targetLead.Score;

        targetLead.MergeFrom(sourceLead, command.Precedence);
        await _leadRepository.UpdateAsync(targetLead, cancellationToken);
        await _leadRepository.DeleteAsync(sourceLead.Id, cancellationToken);

        var history = new List<LeadHistoryEntry>
        {
            LeadHistoryEntry.Create(targetLead.Id, "LeadMerged", "merge_source", sourceLead.Id.ToString(), targetLead.Id.ToString())
        };

        if (!string.Equals(previousAssignedTo, targetLead.AssignedTo, StringComparison.Ordinal))
        {
            history.Add(LeadHistoryEntry.Create(targetLead.Id, "AssignmentChanged", "assigned_to", previousAssignedTo, targetLead.AssignedTo));
        }

        if (previousCampaignId != targetLead.CampaignId)
        {
            history.Add(LeadHistoryEntry.Create(
                targetLead.Id,
                "DataChanged",
                "campaign_id",
                previousCampaignId?.ToString() ?? string.Empty,
                targetLead.CampaignId?.ToString() ?? string.Empty));
        }

        if (previousScore != targetLead.Score)
        {
            history.Add(LeadHistoryEntry.Create(targetLead.Id, "ScoreChanged", "score", previousScore.ToString(), targetLead.Score.ToString()));
        }

        await _leadHistoryRepository.AddRangeAsync(history, cancellationToken);
        await _leadListCache.InvalidateAsync(cancellationToken);

        return targetLead.ToResponse();
    }
}
