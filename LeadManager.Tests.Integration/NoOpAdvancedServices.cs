using LeadManager.Application.Abstractions;
using LeadManager.Domain.Leads;

namespace LeadManager.Tests.Integration;

internal sealed class NoOpOutboxRepository : IOutboxRepository
{
    public Task EnqueueAsync(string eventType, string payloadJson, string idempotencyKey, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyCollection<OutboxMessage>> DequeuePendingAsync(int batchSize, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<OutboxMessage>>([]);

    public Task MarkProcessedAsync(Guid id, DateTime processedAtUtc, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task MarkFailedAsync(Guid id, string errorMessage, int nextRetryInSeconds, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

internal sealed class NoOpAuditTrailRepository : IAuditTrailRepository
{
    public Task WriteInteractionAsync(InteractionAuditRecord record, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task WriteBehaviorEventAsync(BehaviorEventRecord record, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task WriteAiDecisionAsync(AiDecisionRecord record, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

internal sealed class NoOpLeadScoringService : ILeadScoringService
{
    public Task<int?> ScoreAsync(Lead lead, CancellationToken cancellationToken = default) =>
        Task.FromResult<int?>(null);
}

internal sealed class NoOpAssignmentRepository : IAssignmentRepository
{
    public Task AddAsync(Guid leadId, string assignee, string reason, DateTime assignedAtUtc, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
