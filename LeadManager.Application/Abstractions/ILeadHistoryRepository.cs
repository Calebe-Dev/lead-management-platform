using LeadManager.Domain.Leads;

namespace LeadManager.Application.Abstractions;

public interface ILeadHistoryRepository
{
    Task AddRangeAsync(IReadOnlyCollection<LeadHistoryEntry> historyEntries, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<LeadHistoryEntry>> ListByLeadIdAsync(Guid leadId, CancellationToken cancellationToken = default);
}
