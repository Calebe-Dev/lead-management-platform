namespace LeadManager.Application.Leads;

public sealed record PagedLeadHistoryResponse(
    IReadOnlyCollection<LeadHistoryResponse> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);
