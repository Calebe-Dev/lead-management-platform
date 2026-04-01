using LeadManager.Domain.Leads;

namespace LeadManager.Application.Abstractions;

public interface ILeadScoringService
{
    Task<int?> ScoreAsync(Lead lead, CancellationToken cancellationToken = default);
}
