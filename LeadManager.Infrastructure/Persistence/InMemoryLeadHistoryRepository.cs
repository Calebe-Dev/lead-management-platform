using LeadManager.Application.Abstractions;
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

    public Task<IReadOnlyCollection<LeadHistoryEntry>> ListByLeadIdAsync(Guid leadId, CancellationToken cancellationToken = default)
    {
        lock (Sync)
        {
            IReadOnlyCollection<LeadHistoryEntry> items = Entries
                .Where(entry => entry.LeadId == leadId)
                .OrderByDescending(entry => entry.ChangedAtUtc)
                .ToArray();
            return Task.FromResult(items);
        }
    }
}
