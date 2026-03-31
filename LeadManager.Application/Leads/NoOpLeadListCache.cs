using LeadManager.Application.Abstractions;

namespace LeadManager.Application.Leads;

internal sealed class NoOpLeadListCache : ILeadListCache
{
    public Task<ListLeadsResponse?> GetAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            throw new ArgumentException("Cache key is required.", nameof(cacheKey));
        }

        return Task.FromResult<ListLeadsResponse?>(null);
    }

    public Task SetAsync(string cacheKey, ListLeadsResponse response, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            throw new ArgumentException("Cache key is required.", nameof(cacheKey));
        }

        ArgumentNullException.ThrowIfNull(response);
        if (ttl <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ttl), "Cache TTL must be greater than zero.");
        }

        return Task.CompletedTask;
    }

    public Task InvalidateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
