namespace LeadManager.Application.Abstractions;

public interface IWhatsAppIntegrationService
{
    Task SendHotLeadNotificationAsync(Guid leadId, string assignee, string message, CancellationToken cancellationToken = default);
}
