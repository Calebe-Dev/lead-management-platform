using System.Text.Json;
using LeadManager.Application.Abstractions;

namespace LeadManager.Application.Integrations;

public sealed class RecordWebhookEventUseCase
{
    private readonly IOutboxRepository _outboxRepository;

    public RecordWebhookEventUseCase(IOutboxRepository outboxRepository)
    {
        _outboxRepository = outboxRepository;
    }

    public Task ExecuteAsync(string provider, JsonElement payload, CancellationToken cancellationToken = default)
    {
        var normalizedProvider = provider?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedProvider))
        {
            throw new ArgumentException("Webhook provider is required.", nameof(provider));
        }

        return _outboxRepository.EnqueueAsync(
            $"webhook.{normalizedProvider}",
            payload.GetRawText(),
            $"webhook:{normalizedProvider}:{Guid.NewGuid()}",
            cancellationToken);
    }
}
