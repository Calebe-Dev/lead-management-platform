namespace LeadManager.Infrastructure.Persistence;

internal sealed class LeadHistoryRecord
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public DateTime ChangedAtUtc { get; set; }
}
