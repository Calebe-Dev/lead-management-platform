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
            Score = lead.Score,
            Temperature = (int)lead.Temperature,
            Status = (int)lead.Status,
            CreatedAtUtc = lead.CreatedAtUtc
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
            lead.Score,
            (LeadTemperature)lead.Temperature,
            (LeadStatus)lead.Status,
            lead.CreatedAtUtc);

    public static void UpdateFromDomain(this LeadRecord record, Lead lead)
    {
        record.Name = lead.Name;
        record.Email = lead.Email;
        record.Phone = lead.Phone;
        record.Company = lead.Company;
        record.JobTitle = lead.JobTitle;
        record.Source = lead.Source;
        record.Score = lead.Score;
        record.Temperature = (int)lead.Temperature;
        record.Status = (int)lead.Status;
        record.CreatedAtUtc = lead.CreatedAtUtc;
    }
}
