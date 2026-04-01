namespace LeadManager.Application.Campaigns;

public sealed record UpsertCampaignCommand(string Name, string Channel, string Utm, bool IsActive = true);
