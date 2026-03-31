namespace LeadManager.Domain.Leads;

public sealed class LeadHistoryEntry
{
    public Guid Id { get; private set; }
    public Guid LeadId { get; private set; }
    public string EventType { get; private set; }
    public string FieldName { get; private set; }
    public string OldValue { get; private set; }
    public string NewValue { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }

    private LeadHistoryEntry(
        Guid id,
        Guid leadId,
        string eventType,
        string fieldName,
        string oldValue,
        string newValue,
        DateTime changedAtUtc)
    {
        Id = id;
        LeadId = leadId;
        EventType = eventType;
        FieldName = fieldName;
        OldValue = oldValue;
        NewValue = newValue;
        ChangedAtUtc = changedAtUtc;
    }

    public static LeadHistoryEntry Create(
        Guid leadId,
        string eventType,
        string fieldName,
        string oldValue,
        string newValue,
        DateTime? changedAtUtc = null)
    {
        if (leadId == Guid.Empty)
        {
            throw new ArgumentException("Lead id is required.", nameof(leadId));
        }

        var normalizedEventType = (eventType ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedEventType))
        {
            throw new ArgumentException("Event type is required.", nameof(eventType));
        }

        var normalizedFieldName = (fieldName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedFieldName))
        {
            throw new ArgumentException("Field name is required.", nameof(fieldName));
        }

        return new LeadHistoryEntry(
            Guid.NewGuid(),
            leadId,
            normalizedEventType,
            normalizedFieldName,
            oldValue?.Trim() ?? string.Empty,
            newValue?.Trim() ?? string.Empty,
            changedAtUtc ?? DateTime.UtcNow);
    }

    public static LeadHistoryEntry Rehydrate(
        Guid id,
        Guid leadId,
        string eventType,
        string fieldName,
        string oldValue,
        string newValue,
        DateTime changedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("History id is required.", nameof(id));
        }

        if (changedAtUtc == default)
        {
            throw new ArgumentException("History changed date is required.", nameof(changedAtUtc));
        }

        var entry = Create(leadId, eventType, fieldName, oldValue, newValue, DateTime.SpecifyKind(changedAtUtc, DateTimeKind.Utc));
        entry.Id = id;
        return entry;
    }
}
