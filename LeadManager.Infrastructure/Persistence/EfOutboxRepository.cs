using LeadManager.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace LeadManager.Infrastructure.Persistence;

public sealed class EfOutboxRepository : IOutboxRepository
{
    private readonly LeadManagerDbContext _dbContext;

    public EfOutboxRepository(LeadManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task EnqueueAsync(string eventType, string payloadJson, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var normalizedEventType = eventType?.Trim() ?? string.Empty;
        var normalizedIdempotencyKey = idempotencyKey?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedEventType))
        {
            throw new ArgumentException("Event type is required.", nameof(eventType));
        }

        if (string.IsNullOrWhiteSpace(normalizedIdempotencyKey))
        {
            throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));
        }

        var alreadyExists = await _dbContext.OutboxMessages
            .AsNoTracking()
            .AnyAsync(x => x.IdempotencyKey == normalizedIdempotencyKey, cancellationToken);
        if (alreadyExists)
        {
            return;
        }

        _dbContext.OutboxMessages.Add(new OutboxMessageRecord
        {
            Id = Guid.NewGuid(),
            EventType = normalizedEventType,
            PayloadJson = string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson,
            IdempotencyKey = normalizedIdempotencyKey,
            RetryCount = 0,
            CreatedAtUtc = DateTime.UtcNow,
            ProcessedAtUtc = null,
            LastAttemptAtUtc = null,
            NextAttemptAtUtc = null,
            LastError = string.Empty
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<OutboxMessage>> DequeuePendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var records = await _dbContext.OutboxMessages
            .AsNoTracking()
            .Where(x => !x.ProcessedAtUtc.HasValue && (!x.NextAttemptAtUtc.HasValue || x.NextAttemptAtUtc <= now))
            .OrderBy(x => x.CreatedAtUtc)
            .Take(batchSize <= 0 ? 20 : batchSize)
            .ToListAsync(cancellationToken);

        return records.Select(record => new OutboxMessage(
            record.Id,
            record.EventType,
            record.PayloadJson,
            record.IdempotencyKey,
            record.RetryCount)).ToArray();
    }

    public async Task MarkProcessedAsync(Guid id, DateTime processedAtUtc, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.OutboxMessages
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (record is null)
        {
            return;
        }

        record.ProcessedAtUtc = processedAtUtc;
        record.LastAttemptAtUtc = processedAtUtc;
        record.LastError = string.Empty;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(Guid id, string errorMessage, int nextRetryInSeconds, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.OutboxMessages
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (record is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        record.RetryCount++;
        record.LastAttemptAtUtc = now;
        record.NextAttemptAtUtc = now.AddSeconds(Math.Max(5, nextRetryInSeconds));
        record.LastError = errorMessage?.Trim() ?? "unknown_error";
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
