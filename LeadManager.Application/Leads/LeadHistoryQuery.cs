namespace LeadManager.Application.Leads;

public sealed class LeadHistoryQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
