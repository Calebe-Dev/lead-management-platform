namespace LeadManager.Application.Leads;

public sealed record CreateLeadCommand(
    string Name,
    string Email,
    string Phone,
    string Company,
    string JobTitle,
    string Source);
