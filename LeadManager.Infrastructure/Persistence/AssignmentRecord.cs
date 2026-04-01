namespace LeadManager.Infrastructure.Persistence;

internal sealed class AssignmentRecord
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public string Assignee { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime AssignedAtUtc { get; set; }
}
