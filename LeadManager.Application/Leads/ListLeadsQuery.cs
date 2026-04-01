using LeadManager.Domain.Leads;

namespace LeadManager.Application.Leads;

public sealed class ListLeadsQuery
{
    public LeadStatus? Status { get; init; }
    public LeadTemperature? Temperature { get; init; }
    public string? Region { get; init; }
    public string? LeadType { get; init; }
    public string? ProductInterest { get; init; }
    public string? AssignedTo { get; init; }
    public string? Search { get; init; }
    public Guid? CampaignId { get; init; }
    public int? MinScore { get; init; }
    public int? MaxScore { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
