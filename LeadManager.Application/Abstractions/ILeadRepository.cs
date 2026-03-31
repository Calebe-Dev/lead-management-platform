using LeadManager.Domain.Leads;

namespace LeadManager.Application.Abstractions;

public interface ILeadRepository
{
    Task AddAsync(Lead lead, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Lead>> ListAsync(CancellationToken cancellationToken = default);
}
