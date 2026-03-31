using LeadManager.Application.Abstractions;
using LeadManager.Domain.Leads;

namespace LeadManager.Infrastructure.Persistence;

public sealed class InMemoryLeadRepository : ILeadRepository
{
    private static readonly List<Lead> Leads = [];

    public Task AddAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        Leads.Add(lead);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<Lead>> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Lead> snapshot = Leads.ToArray();
        return Task.FromResult(snapshot);
    }
}
