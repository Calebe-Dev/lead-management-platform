namespace LeadManager.Application.Leads;

public sealed record LeadResponse(
    Guid Id,
    string Name,
    string Email,
    string Phone,
    string Company,
    string JobTitle,
    string Source,
    string Region,
    string LeadType,
    string ProductInterest,
    string Cnpj,
    string AssignedTo,
    Guid? CampaignId,
    int Score,
    string Temperature,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
