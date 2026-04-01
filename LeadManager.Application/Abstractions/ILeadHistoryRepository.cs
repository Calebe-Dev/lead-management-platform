using LeadManager.Application.Leads;
using LeadManager.Domain.Leads;

namespace LeadManager.Application.Abstractions;

public interface ILeadHistoryRepository
{
    Task AddRangeAsync(IReadOnlyCollection<LeadHistoryEntry> historyEntries, CancellationToken cancellationToken = default);
    Task<PagedResult<LeadHistoryEntry>> ListByLeadIdAsync(Guid leadId, int page, int pageSize, CancellationToken cancellationToken = default);
}
