using LeadManager.Domain.Leads;

namespace LeadManager.Infrastructure.Persistence;

internal static class LeadHistoryRecordMappings
{
    public static LeadHistoryRecord ToRecord(this LeadHistoryEntry entry) =>
        new()
        {
            Id = entry.Id,
            LeadId = entry.LeadId,
            EventType = entry.EventType,
            FieldName = entry.FieldName,
            OldValue = entry.OldValue,
            NewValue = entry.NewValue,
            ChangedAtUtc = entry.ChangedAtUtc
        };

    public static LeadHistoryEntry ToDomain(this LeadHistoryRecord record) =>
        LeadHistoryEntry.Rehydrate(
            record.Id,
            record.LeadId,
            record.EventType,
            record.FieldName,
            record.OldValue,
            record.NewValue,
            record.ChangedAtUtc);
}
