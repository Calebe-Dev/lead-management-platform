using LeadManager.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace LeadManager.Infrastructure.Persistence;

public sealed class EfRoundRobinStateRepository : IRoundRobinStateRepository
{
    private readonly LeadManagerDbContext _dbContext;

    public EfRoundRobinStateRepository(LeadManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> GetNextIndexAsync(string key, int poolSize, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Round robin key is required.", nameof(key));
        }

        if (poolSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(poolSize), "Pool size must be greater than zero.");
        }

        var normalizedKey = key.Trim();
        var state = await _dbContext.RoundRobinStates.FirstOrDefaultAsync(x => x.Key == normalizedKey, cancellationToken);

        if (state is null)
        {
            state = new RoundRobinStateRecord { Key = normalizedKey, LastIndex = 0 };
            _dbContext.RoundRobinStates.Add(state);
        }
        else
        {
            state.LastIndex = (state.LastIndex + 1) % poolSize;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return state.LastIndex;
    }
}
