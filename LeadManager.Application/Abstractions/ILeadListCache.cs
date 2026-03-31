using LeadManager.Application.Leads;

namespace LeadManager.Application.Abstractions;

public interface ILeadListCache
{
    Task<IReadOnlyCollection<LeadResponse>?> GetAsync(CancellationToken cancellationToken = default);
    Task SetAsync(IReadOnlyCollection<LeadResponse> leads, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task InvalidateAsync(CancellationToken cancellationToken = default);
}
