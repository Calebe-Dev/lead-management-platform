using LeadManager.Domain.Leads;

namespace LeadManager.Application.Leads;

internal static class LeadMappings
{
    public static LeadResponse ToResponse(this Lead lead) =>
        new(
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
            lead.Temperature.ToString(),
            lead.Status.ToString(),
            lead.CreatedAtUtc,
            lead.UpdatedAtUtc);
}
