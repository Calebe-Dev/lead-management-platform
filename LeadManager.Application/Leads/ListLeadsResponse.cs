namespace LeadManager.Application.Leads;

public sealed record ListLeadsResponse(
    IReadOnlyCollection<LeadResponse> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);
