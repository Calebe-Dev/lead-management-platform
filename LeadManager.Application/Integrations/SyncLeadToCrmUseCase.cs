using LeadManager.Application.Abstractions;
using LeadManager.Application.Leads;

namespace LeadManager.Application.Integrations;

public sealed class SyncLeadToCrmUseCase
{
    private readonly ILeadRepository _leadRepository;
    private readonly ICrmIntegrationService _crmIntegrationService;
    private readonly IOutboxRepository _outboxRepository;

    public SyncLeadToCrmUseCase(
        ILeadRepository leadRepository,
        ICrmIntegrationService crmIntegrationService,
        IOutboxRepository outboxRepository)
    {
        _leadRepository = leadRepository;
        _crmIntegrationService = crmIntegrationService;
        _outboxRepository = outboxRepository;
    }

    public async Task<LeadResponse?> ExecuteAsync(Guid leadId, CancellationToken cancellationToken = default)
    {
        if (leadId == Guid.Empty)
        {
            throw new ArgumentException("Lead id is required.", nameof(leadId));
        }

        var lead = await _leadRepository.GetByIdAsync(leadId, cancellationToken);
        if (lead is null)
        {
            return null;
        }

        var response = lead.ToResponse();
        await _crmIntegrationService.SyncToHubSpotAsync(response, cancellationToken);
        await _crmIntegrationService.SyncToSalesforceAsync(response, cancellationToken);
        await _outboxRepository.EnqueueAsync(
            "crm.sync.completed",
            $$"""
            {"leadId":"{{leadId}}","providers":["hubspot","salesforce"]}
            """,
            $"crm-sync:{leadId}:{DateTime.UtcNow:yyyyMMddHHmmss}",
            cancellationToken);

        return response;
    }
}
