namespace LeadManager.Infrastructure.Auditing;

public sealed class MongoAuditOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string Database { get; set; } = "lead_manager_audit";
    public bool Enabled { get; set; }
}
