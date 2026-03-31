using LeadManager.Application.Leads;

namespace LeadManager.Application.Abstractions;

public interface ILeadListCache
{
    Task<ListLeadsResponse?> GetAsync(string cacheKey, CancellationToken cancellationToken = default);
    Task SetAsync(string cacheKey, ListLeadsResponse response, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task InvalidateAsync(CancellationToken cancellationToken = default);
}
