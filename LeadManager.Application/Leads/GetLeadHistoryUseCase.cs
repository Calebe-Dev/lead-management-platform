using LeadManager.Application.Abstractions;

namespace LeadManager.Application.Leads;

public sealed class GetLeadHistoryUseCase
{
    private readonly ILeadHistoryRepository _leadHistoryRepository;

    public GetLeadHistoryUseCase(ILeadHistoryRepository leadHistoryRepository)
    {
        _leadHistoryRepository = leadHistoryRepository;
    }

    public async Task<IReadOnlyCollection<LeadHistoryResponse>> ExecuteAsync(Guid leadId, CancellationToken cancellationToken = default)
    {
        if (leadId == Guid.Empty)
        {
            throw new ArgumentException("Lead id is required.", nameof(leadId));
        }

        var history = await _leadHistoryRepository.ListByLeadIdAsync(leadId, cancellationToken);
        return history.Select(entry => new LeadHistoryResponse(
            entry.Id,
            entry.LeadId,
            entry.EventType,
            entry.FieldName,
            entry.OldValue,
            entry.NewValue,
            entry.ChangedAtUtc)).ToArray();
    }
}
