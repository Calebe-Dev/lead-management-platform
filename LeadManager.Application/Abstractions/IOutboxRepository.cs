namespace LeadManager.Application.Abstractions;

public interface IOutboxRepository
{
    Task EnqueueAsync(string eventType, string payloadJson, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<OutboxMessage>> DequeuePendingAsync(int batchSize, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(Guid id, DateTime processedAtUtc, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(Guid id, string errorMessage, int nextRetryInSeconds, CancellationToken cancellationToken = default);
}

public sealed record OutboxMessage(
    Guid Id,
    string EventType,
    string PayloadJson,
    string IdempotencyKey,
    int RetryCount);
