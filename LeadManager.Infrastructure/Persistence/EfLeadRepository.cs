using LeadManager.Application.Abstractions;
using LeadManager.Domain.Leads;
using Microsoft.EntityFrameworkCore;

namespace LeadManager.Infrastructure.Persistence;

public sealed class EfLeadRepository : ILeadRepository
{
    private readonly LeadManagerDbContext _dbContext;

    public EfLeadRepository(LeadManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lead);
        _dbContext.Leads.Add(lead.ToRecord());
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Lead?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Lead id is required.", nameof(id));
        }

        var leadRecord = await _dbContext.Leads
            .AsNoTracking()
            .FirstOrDefaultAsync(lead => lead.Id == id, cancellationToken);

        return leadRecord?.ToDomain();
    }

    public async Task<IReadOnlyCollection<Lead>> ListAsync(CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.Leads
            .AsNoTracking()
            .OrderByDescending(lead => lead.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return records.Select(lead => lead.ToDomain()).ToArray();
    }

    public async Task UpdateAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lead);

        var existingRecord = await _dbContext.Leads
            .FirstOrDefaultAsync(existingLead => existingLead.Id == lead.Id, cancellationToken);

        if (existingRecord is null)
        {
            throw new InvalidOperationException($"Lead with id '{lead.Id}' was not found for update.");
        }

        existingRecord.UpdateFromDomain(lead);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
