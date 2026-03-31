namespace LeadManager.Application.Abstractions;

public interface IRoundRobinStateRepository
{
    Task<int> GetNextIndexAsync(string key, int poolSize, CancellationToken cancellationToken = default);
}
