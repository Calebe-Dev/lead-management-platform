using LeadManager.Application.Abstractions;

namespace LeadManager.Application.Leads;

internal sealed class NoOpLeadListCache : ILeadListCache
{
    public Task<IReadOnlyCollection<LeadResponse>?> GetAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<LeadResponse>?>(null);

    public Task SetAsync(IReadOnlyCollection<LeadResponse> leads, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(leads);
        return Task.CompletedTask;
    }

    public Task InvalidateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
