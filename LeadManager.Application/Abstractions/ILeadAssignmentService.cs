using LeadManager.Domain.Leads;

namespace LeadManager.Application.Abstractions;

public interface ILeadAssignmentService
{
    Task<string?> SelectAssigneeAsync(Lead lead, CancellationToken cancellationToken = default);
}
