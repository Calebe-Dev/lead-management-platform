namespace LeadManager.Application.Campaigns;

public sealed record CampaignResponse(
    Guid Id,
    string Name,
    string Channel,
    string Utm,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
