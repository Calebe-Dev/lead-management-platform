using LeadManager.Application.Abstractions;

namespace LeadManager.Tests.Integration;

internal sealed class InMemoryRoundRobinStateRepository : IRoundRobinStateRepository
{
    private readonly Dictionary<string, int> _state = new(StringComparer.Ordinal);

    public Task<int> GetNextIndexAsync(string key, int poolSize, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Round robin key is required.", nameof(key));
        }

        if (poolSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(poolSize), "Pool size must be greater than zero.");
        }

        if (!_state.TryGetValue(key, out var current))
        {
            _state[key] = 0;
            return Task.FromResult(0);
        }

        var next = (current + 1) % poolSize;
        _state[key] = next;
        return Task.FromResult(next);
    }
}
