using LeadManager.Application.Leads;
using LeadManager.Domain.Leads;

namespace LeadManager.Application.Abstractions;

public interface ILeadRepository
{
    Task AddAsync(Lead lead, CancellationToken cancellationToken = default);
    Task<Lead?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DuplicateLeadMatch?> FindDuplicateAsync(string email, string phone, string cnpj, CancellationToken cancellationToken = default);
    Task<PagedResult<Lead>> ListAsync(ListLeadsQuery query, CancellationToken cancellationToken = default);
    Task UpdateAsync(Lead lead, CancellationToken cancellationToken = default);
}
