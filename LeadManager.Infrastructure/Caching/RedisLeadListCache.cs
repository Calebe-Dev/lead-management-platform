using System.Text.Json;
using LeadManager.Application.Abstractions;
using LeadManager.Application.Leads;
using Microsoft.Extensions.Caching.Distributed;

namespace LeadManager.Infrastructure.Caching;

public sealed class RedisLeadListCache : ILeadListCache
{
    private const string VersionCacheKey = "leads:list:version";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IDistributedCache _cache;

    public RedisLeadListCache(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<ListLeadsResponse?> GetAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            throw new ArgumentException("Cache key is required.", nameof(cacheKey));
        }

        var version = await GetVersionAsync(cancellationToken);
        var payload = await _cache.GetStringAsync(BuildVersionedKey(cacheKey, version), cancellationToken);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ListLeadsResponse>(payload, SerializerOptions);
    }

    public async Task SetAsync(string cacheKey, ListLeadsResponse response, TimeSpan ttl, CancellationToken cancellationToken = default)
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

        var version = await GetVersionAsync(cancellationToken);
        var payload = JsonSerializer.Serialize(response, SerializerOptions);
        await _cache.SetStringAsync(
            BuildVersionedKey(cacheKey, version),
            payload,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            },
            cancellationToken);
    }

    public async Task InvalidateAsync(CancellationToken cancellationToken = default)
    {
        var version = await GetVersionAsync(cancellationToken);
        await _cache.SetStringAsync(
            VersionCacheKey,
            (version + 1).ToString(),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30) },
            cancellationToken);
    }

    private async Task<int> GetVersionAsync(CancellationToken cancellationToken)
    {
        var rawVersion = await _cache.GetStringAsync(VersionCacheKey, cancellationToken);
        return int.TryParse(rawVersion, out var parsedVersion) && parsedVersion >= 0 ? parsedVersion : 0;
    }

    private static string BuildVersionedKey(string cacheKey, int version) => $"{cacheKey}:v{version}";
}
