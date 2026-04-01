using LeadManager.Application.Abstractions;
using LeadManager.Domain.Leads;

namespace LeadManager.Application.Leads;

public sealed class UpdateLeadStatusUseCase
{
    private readonly ILeadRepository _leadRepository;
    private readonly ILeadHistoryRepository _leadHistoryRepository;
    private readonly ILeadListCache _leadListCache;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IAuditTrailRepository _auditTrailRepository;

    public UpdateLeadStatusUseCase(
        ILeadRepository leadRepository,
        ILeadHistoryRepository leadHistoryRepository,
        ILeadListCache leadListCache,
        IOutboxRepository outboxRepository,
        IAuditTrailRepository auditTrailRepository)
    {
        _leadRepository = leadRepository;
        _leadHistoryRepository = leadHistoryRepository;
        _leadListCache = leadListCache;
        _outboxRepository = outboxRepository;
        _auditTrailRepository = auditTrailRepository;
    }

    public async Task<LeadResponse?> ExecuteAsync(Guid id, UpdateLeadStatusCommand command, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Lead id is required.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(command);

        var lead = await _leadRepository.GetByIdAsync(id, cancellationToken);
        if (lead is null)
        {
            return null;
        }

        var previousStatus = lead.Status;
        lead.ChangeStatus(command.Status);
        await _leadRepository.UpdateAsync(lead, cancellationToken);

        if (previousStatus != lead.Status)
        {
            await _leadHistoryRepository.AddRangeAsync(
                [LeadHistoryEntry.Create(lead.Id, "StatusChanged", "status", previousStatus.ToString(), lead.Status.ToString())],
                cancellationToken);
            await _auditTrailRepository.WriteInteractionAsync(
                new InteractionAuditRecord(
                    lead.Id,
                    "lead_status_changed",
                    $$"""
                    {"oldStatus":"{{previousStatus}}","newStatus":"{{lead.Status}}"}
                    """,
                    DateTime.UtcNow),
                cancellationToken);
            await _outboxRepository.EnqueueAsync(
                "lead.status.changed",
                $$"""
                {"leadId":"{{lead.Id}}","oldStatus":"{{previousStatus}}","newStatus":"{{lead.Status}}"}
                """,
                $"lead-status:{lead.Id}:{lead.Status}",
                cancellationToken);
        }

        await _leadListCache.InvalidateAsync(cancellationToken);

        return lead.ToResponse();
    }
}
