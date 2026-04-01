namespace LeadManager.Infrastructure.Integrations;

public sealed class IntegrationOptions
{
    public CrmProviderOptions HubSpot { get; init; } = new();
    public CrmProviderOptions Salesforce { get; init; } = new();
    public WhatsAppProviderOptions WhatsAppMeta { get; init; } = new();
    public WhatsAppProviderOptions WhatsAppTwilio { get; init; } = new();
}

public sealed class CrmProviderOptions
{
    public bool Enabled { get; init; }
    public string BaseUrl { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
}

public sealed class WhatsAppProviderOptions
{
    public bool Enabled { get; init; }
    public string BaseUrl { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string Sender { get; init; } = string.Empty;
}
