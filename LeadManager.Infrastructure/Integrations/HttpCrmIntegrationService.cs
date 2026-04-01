using System.Net.Http.Headers;
using System.Net.Http.Json;
using LeadManager.Application.Abstractions;
using LeadManager.Application.Leads;
using Microsoft.Extensions.Options;

namespace LeadManager.Infrastructure.Integrations;

public sealed class HttpCrmIntegrationService : ICrmIntegrationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IntegrationOptions _options;

    public HttpCrmIntegrationService(IHttpClientFactory httpClientFactory, IOptions<IntegrationOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public Task SyncToHubSpotAsync(LeadResponse lead, CancellationToken cancellationToken = default) =>
        SendAsync(_options.HubSpot, "hubspot", lead, cancellationToken);

    public Task SyncToSalesforceAsync(LeadResponse lead, CancellationToken cancellationToken = default) =>
        SendAsync(_options.Salesforce, "salesforce", lead, cancellationToken);

    private async Task SendAsync(CrmProviderOptions provider, string providerName, LeadResponse lead, CancellationToken cancellationToken)
    {
        if (!provider.Enabled || string.IsNullOrWhiteSpace(provider.BaseUrl) || string.IsNullOrWhiteSpace(provider.ApiKey))
        {
            return;
        }

        var endpoint = provider.BaseUrl.TrimEnd('/') + "/leads/sync";
        var payload = new
        {
            id = lead.Id,
            name = lead.Name,
            email = lead.Email,
            phone = lead.Phone,
            company = lead.Company,
            source = lead.Source,
            score = lead.Score,
            temperature = lead.Temperature,
            status = lead.Status,
            campaignId = lead.CampaignId
        };

        Exception? lastException = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var client = _httpClientFactory.CreateClient(nameof(HttpCrmIntegrationService));
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", provider.ApiKey);
                request.Headers.Add("X-Provider", providerName);
                request.Content = JsonContent.Create(payload);
                using var response = await client.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                if ((int)response.StatusCode < 500)
                {
                    return;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                lastException = ex;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken);
        }

        if (lastException is not null)
        {
            throw new InvalidOperationException($"CRM sync failed for provider '{providerName}'.", lastException);
        }
    }
}
