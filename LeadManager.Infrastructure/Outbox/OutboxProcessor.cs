using System.Text.Json;
using LeadManager.Application.Abstractions;
using LeadManager.Application.Leads;

namespace LeadManager.Infrastructure.Outbox;

public sealed class OutboxProcessor
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly ICrmIntegrationService _crmIntegrationService;
    private readonly IWhatsAppIntegrationService _whatsAppIntegrationService;

    public OutboxProcessor(
        IOutboxRepository outboxRepository,
        ICrmIntegrationService crmIntegrationService,
        IWhatsAppIntegrationService whatsAppIntegrationService)
    {
        _outboxRepository = outboxRepository;
        _crmIntegrationService = crmIntegrationService;
        _whatsAppIntegrationService = whatsAppIntegrationService;
    }

    public async Task<int> ProcessBatchAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var pending = await _outboxRepository.DequeuePendingAsync(batchSize, cancellationToken);
        var processed = 0;

        foreach (var message in pending)
        {
            try
            {
                await DispatchAsync(message, cancellationToken);
                await _outboxRepository.MarkProcessedAsync(message.Id, DateTime.UtcNow, cancellationToken);
                processed++;
            }
            catch (Exception ex)
            {
                var retryInSeconds = (int)Math.Min(300, Math.Pow(2, message.RetryCount + 1) * 5);
                await _outboxRepository.MarkFailedAsync(message.Id, ex.Message, retryInSeconds, cancellationToken);
            }
        }

        return processed;
    }

    private async Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        switch (message.EventType)
        {
            case "lead.hot":
            {
                using var document = JsonDocument.Parse(message.PayloadJson);
                var root = document.RootElement;
                var leadId = root.GetProperty("leadId").GetGuid();
                var assignedTo = root.TryGetProperty("assignedTo", out var assigneeElement)
                    ? assigneeElement.GetString() ?? string.Empty
                    : string.Empty;
                var score = root.TryGetProperty("score", out var scoreElement) ? scoreElement.GetInt32() : 0;
                await _whatsAppIntegrationService.SendHotLeadNotificationAsync(
                    leadId,
                    assignedTo,
                    $"Lead quente atribuído: {leadId} (score {score}).",
                    cancellationToken);
                return;
            }
            case "crm.sync.completed":
            case "lead.created":
            case "lead.status.changed":
            case "lead.score.changed":
            case "webhook.hubspot":
            case "webhook.salesforce":
            case "webhook.whatsapp":
                return;
            case "crm.sync.requested":
            {
                using var document = JsonDocument.Parse(message.PayloadJson);
                var root = document.RootElement;
                var lead = new LeadResponse(
                    root.GetProperty("id").GetGuid(),
                    root.GetProperty("name").GetString() ?? string.Empty,
                    root.GetProperty("email").GetString() ?? string.Empty,
                    root.GetProperty("phone").GetString() ?? string.Empty,
                    root.GetProperty("company").GetString() ?? string.Empty,
                    root.GetProperty("jobTitle").GetString() ?? string.Empty,
                    root.GetProperty("source").GetString() ?? string.Empty,
                    root.GetProperty("region").GetString() ?? string.Empty,
                    root.GetProperty("leadType").GetString() ?? string.Empty,
                    root.GetProperty("productInterest").GetString() ?? string.Empty,
                    root.GetProperty("cnpj").GetString() ?? string.Empty,
                    root.GetProperty("assignedTo").GetString() ?? string.Empty,
                    root.TryGetProperty("campaignId", out var campaignElement) && campaignElement.ValueKind != JsonValueKind.Null
                        ? campaignElement.GetGuid()
                        : null,
                    root.GetProperty("score").GetInt32(),
                    root.GetProperty("temperature").GetString() ?? "Cold",
                    root.GetProperty("status").GetString() ?? "New",
                    root.GetProperty("createdAtUtc").GetDateTime(),
                    root.GetProperty("updatedAtUtc").GetDateTime());
                await _crmIntegrationService.SyncToHubSpotAsync(lead, cancellationToken);
                await _crmIntegrationService.SyncToSalesforceAsync(lead, cancellationToken);
                return;
            }
            default:
                return;
        }
    }
}
