namespace LeadManager.Infrastructure.Auditing;

public sealed class MongoAuditOptions
{
    public string ConnectionString { get; init; } = string.Empty;
    public string Database { get; init; } = "lead_manager_audit";
    public bool Enabled { get; init; }
}
