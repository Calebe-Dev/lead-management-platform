namespace LeadManager.Application.Leads;

public sealed record LeadHistoryResponse(
    Guid Id,
    Guid LeadId,
    string EventType,
    string FieldName,
    string OldValue,
    string NewValue,
    DateTime ChangedAtUtc);
