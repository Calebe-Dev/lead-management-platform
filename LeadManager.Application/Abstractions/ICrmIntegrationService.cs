using LeadManager.Application.Leads;

namespace LeadManager.Application.Abstractions;

public interface ICrmIntegrationService
{
    Task SyncToHubSpotAsync(LeadResponse lead, CancellationToken cancellationToken = default);
    Task SyncToSalesforceAsync(LeadResponse lead, CancellationToken cancellationToken = default);
}
