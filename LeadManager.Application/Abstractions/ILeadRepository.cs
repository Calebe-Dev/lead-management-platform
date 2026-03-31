using LeadManager.Domain.Leads;

namespace LeadManager.Application.Abstractions;

public interface ILeadRepository
{
    Task AddAsync(Lead lead, CancellationToken cancellationToken = default);
    Task<Lead?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Lead>> ListAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(Lead lead, CancellationToken cancellationToken = default);
}
