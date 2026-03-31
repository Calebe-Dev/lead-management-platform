using LeadManager.Application.Abstractions;
using LeadManager.Domain.Leads;

namespace LeadManager.Infrastructure.Persistence;

public sealed class InMemoryLeadRepository : ILeadRepository
{
    private static readonly List<Lead> Leads = [];
    private static readonly Lock Sync = new();

    public Task AddAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        lock (Sync)
        {
            Leads.Add(lead);
        }

        return Task.CompletedTask;
    }

    public Task<Lead?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (Sync)
        {
            var lead = Leads.FirstOrDefault(existingLead => existingLead.Id == id);
            return Task.FromResult(lead);
        }
    }

    public Task<IReadOnlyCollection<Lead>> ListAsync(CancellationToken cancellationToken = default)
    {
        lock (Sync)
        {
            IReadOnlyCollection<Lead> snapshot = Leads.ToArray();
            return Task.FromResult(snapshot);
        }
    }

    public Task UpdateAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        lock (Sync)
        {
            var index = Leads.FindIndex(existingLead => existingLead.Id == lead.Id);
            if (index >= 0)
            {
                Leads[index] = lead;
            }
        }

        return Task.CompletedTask;
    }
}
