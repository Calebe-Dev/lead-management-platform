namespace LeadManager.Infrastructure.Persistence;

internal sealed class LeadRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string LeadType { get; set; } = string.Empty;
    public string ProductInterest { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
    public int Score { get; set; }
    public int Temperature { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
