using LeadManager.Domain.Leads;

namespace LeadManager.Infrastructure.Persistence;

internal static class LeadRecordMappings
{
    public static LeadRecord ToRecord(this Lead lead) =>
        new()
        {
            Id = lead.Id,
            Name = lead.Name,
            Email = lead.Email,
            Phone = lead.Phone,
            Company = lead.Company,
            JobTitle = lead.JobTitle,
            Source = lead.Source,
            Region = lead.Region,
            LeadType = lead.LeadType,
            ProductInterest = lead.ProductInterest,
            Cnpj = lead.Cnpj,
            AssignedTo = lead.AssignedTo,
            CampaignId = lead.CampaignId,
            Score = lead.Score,
            Temperature = (int)lead.Temperature,
            Status = (int)lead.Status,
            CreatedAtUtc = lead.CreatedAtUtc,
            UpdatedAtUtc = lead.UpdatedAtUtc
        };

    public static Lead ToDomain(this LeadRecord lead) =>
        Lead.Rehydrate(
            lead.Id,
            lead.Name,
            lead.Email,
            lead.Phone,
            lead.Company,
            lead.JobTitle,
            lead.Source,
            lead.Region,
            lead.LeadType,
            lead.ProductInterest,
            lead.Cnpj,
            lead.AssignedTo,
            lead.CampaignId,
            lead.Score,
            (LeadTemperature)lead.Temperature,
            (LeadStatus)lead.Status,
            lead.CreatedAtUtc,
            lead.UpdatedAtUtc);

    public static void UpdateFromDomain(this LeadRecord record, Lead lead)
    {
        record.Name = lead.Name;
        record.Email = lead.Email;
        record.Phone = lead.Phone;
        record.Company = lead.Company;
        record.JobTitle = lead.JobTitle;
        record.Source = lead.Source;
        record.Region = lead.Region;
        record.LeadType = lead.LeadType;
        record.ProductInterest = lead.ProductInterest;
        record.Cnpj = lead.Cnpj;
        record.AssignedTo = lead.AssignedTo;
        record.CampaignId = lead.CampaignId;
        record.Score = lead.Score;
        record.Temperature = (int)lead.Temperature;
        record.Status = (int)lead.Status;
        record.CreatedAtUtc = lead.CreatedAtUtc;
        record.UpdatedAtUtc = lead.UpdatedAtUtc;
    }
}
