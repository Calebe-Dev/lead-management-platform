using LeadManager.Application.Abstractions;
using LeadManager.Application.Leads;
using LeadManager.Domain.Leads;
using Microsoft.EntityFrameworkCore;

namespace LeadManager.Infrastructure.Persistence;

public sealed class EfLeadHistoryRepository : ILeadHistoryRepository
{
    private readonly LeadManagerDbContext _dbContext;

    public EfLeadHistoryRepository(LeadManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddRangeAsync(IReadOnlyCollection<LeadHistoryEntry> historyEntries, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(historyEntries);
        if (historyEntries.Count == 0)
        {
            return;
        }

        _dbContext.LeadHistory.AddRange(historyEntries.Select(entry => entry.ToRecord()));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<LeadHistoryEntry>> ListByLeadIdAsync(
        Guid leadId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (leadId == Guid.Empty)
        {
            throw new ArgumentException("Lead id is required.", nameof(leadId));
        }

        var query = _dbContext.LeadHistory
            .AsNoTracking()
            .Where(entry => entry.LeadId == leadId)
            .OrderByDescending(entry => entry.ChangedAtUtc);

        var totalItems = await query.CountAsync(cancellationToken);
        var records = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<LeadHistoryEntry>(
            records.Select(entry => entry.ToDomain()).ToArray(),
            page,
            pageSize,
            totalItems);
    }
}
