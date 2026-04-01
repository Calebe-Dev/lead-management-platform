namespace LeadManager.Application.Abstractions;

public interface IAuditTrailRepository
{
    Task WriteInteractionAsync(InteractionAuditRecord record, CancellationToken cancellationToken = default);
    Task WriteBehaviorEventAsync(BehaviorEventRecord record, CancellationToken cancellationToken = default);
    Task WriteAiDecisionAsync(AiDecisionRecord record, CancellationToken cancellationToken = default);
}

public sealed record InteractionAuditRecord(
    Guid LeadId,
    string EventType,
    string PayloadJson,
    DateTime OccurredAtUtc);

public sealed record BehaviorEventRecord(
    Guid LeadId,
    string EventName,
    int ScoreImpact,
    string PayloadJson,
    DateTime OccurredAtUtc);

public sealed record AiDecisionRecord(
    Guid LeadId,
    string Provider,
    string PromptFingerprint,
    string PayloadJson,
    DateTime OccurredAtUtc);
