using LeadManager.Application.Abstractions;
using LeadManager.Application.Leads;
using LeadManager.Domain.Leads;

namespace LeadManager.Infrastructure.Persistence;

public sealed class InMemoryLeadHistoryRepository : ILeadHistoryRepository
{
    private static readonly List<LeadHistoryEntry> Entries = [];
    private static readonly Lock Sync = new();

    public Task AddRangeAsync(IReadOnlyCollection<LeadHistoryEntry> historyEntries, CancellationToken cancellationToken = default)
    {
        lock (Sync)
        {
            Entries.AddRange(historyEntries);
        }

        return Task.CompletedTask;
    }

    public Task<PagedResult<LeadHistoryEntry>> ListByLeadIdAsync(
        Guid leadId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        lock (Sync)
        {
            var items = Entries
                .Where(entry => entry.LeadId == leadId)
                .OrderByDescending(entry => entry.ChangedAtUtc)
                .ToArray();
            var total = items.Length;
            var pageItems = items
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToArray();

            return Task.FromResult(new PagedResult<LeadHistoryEntry>(pageItems, page, pageSize, total));
        }
    }
}
