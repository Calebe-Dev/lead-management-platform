namespace LeadManager.Application.Leads;

public sealed record LeadResponse(
    Guid Id,
    string Name,
    string Email,
    string Phone,
    string Company,
    string JobTitle,
    string Source,
    int Score,
    string Temperature,
    string Status,
    DateTime CreatedAtUtc);
