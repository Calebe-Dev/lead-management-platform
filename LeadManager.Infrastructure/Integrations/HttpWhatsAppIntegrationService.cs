using System.Net.Http.Headers;
using System.Net.Http.Json;
using LeadManager.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace LeadManager.Infrastructure.Integrations;

public sealed class HttpWhatsAppIntegrationService : IWhatsAppIntegrationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IntegrationOptions _options;

    public HttpWhatsAppIntegrationService(IHttpClientFactory httpClientFactory, IOptions<IntegrationOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task SendHotLeadNotificationAsync(Guid leadId, string assignee, string message, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(assignee))
        {
            await SendAsync(_options.WhatsAppMeta, "meta", leadId, assignee, message, cancellationToken);
            await SendAsync(_options.WhatsAppTwilio, "twilio", leadId, assignee, message, cancellationToken);
        }
    }

    private async Task SendAsync(
        WhatsAppProviderOptions provider,
        string providerName,
        Guid leadId,
        string assignee,
        string message,
        CancellationToken cancellationToken)
    {
        if (!provider.Enabled || string.IsNullOrWhiteSpace(provider.BaseUrl) || string.IsNullOrWhiteSpace(provider.ApiKey))
        {
            return;
        }

        var endpoint = provider.BaseUrl.TrimEnd('/') + "/messages";
        var payload = new
        {
            provider = providerName,
            sender = provider.Sender,
            recipient = assignee,
            text = message,
            leadId
        };

        var client = _httpClientFactory.CreateClient(nameof(HttpWhatsAppIntegrationService));
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", provider.ApiKey);
        request.Content = JsonContent.Create(payload);
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
