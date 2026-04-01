using LeadManager.Application.Abstractions;

namespace LeadManager.Application.Leads;

public sealed class GetLeadHistoryUseCase
{
    private readonly ILeadHistoryRepository _leadHistoryRepository;

    public GetLeadHistoryUseCase(ILeadHistoryRepository leadHistoryRepository)
    {
        _leadHistoryRepository = leadHistoryRepository;
    }

    public async Task<PagedLeadHistoryResponse> ExecuteAsync(
        Guid leadId,
        LeadHistoryQuery? query,
        CancellationToken cancellationToken = default)
    {
        if (leadId == Guid.Empty)
        {
            throw new ArgumentException("Lead id is required.", nameof(leadId));
        }

        query ??= new LeadHistoryQuery();
        if (query.Page <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(query.Page), "Page must be greater than zero.");
        }

        if (query.PageSize <= 0 || query.PageSize > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(query.PageSize), "Page size must be between 1 and 200.");
        }

        var history = await _leadHistoryRepository.ListByLeadIdAsync(leadId, query.Page, query.PageSize, cancellationToken);
        return new PagedLeadHistoryResponse(
            history.Items
                .Select(entry => new LeadHistoryResponse(
                    entry.Id,
                    entry.LeadId,
                    entry.EventType,
                    entry.FieldName,
                    entry.OldValue,
                    entry.NewValue,
                    entry.ChangedAtUtc))
                .ToArray(),
            history.Page,
            history.PageSize,
            history.TotalItems,
            history.TotalPages);
    }
}
