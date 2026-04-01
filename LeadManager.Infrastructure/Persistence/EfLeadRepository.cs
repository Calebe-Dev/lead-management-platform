using LeadManager.Application.Abstractions;
using LeadManager.Application.Leads;
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

    public async Task<DuplicateLeadMatch?> FindDuplicateAsync(string email, string phone, string cnpj, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedPhone = NormalizeDigits(phone);
        var normalizedCnpj = NormalizeDigits(cnpj);

        var duplicate = await _dbContext.Leads
            .AsNoTracking()
            .FirstOrDefaultAsync(lead =>
                    lead.Email == normalizedEmail
                    || lead.Phone == normalizedPhone
                    || (!string.IsNullOrWhiteSpace(normalizedCnpj) && lead.Cnpj == normalizedCnpj),
                cancellationToken);

        if (duplicate is null)
        {
            return null;
        }

        var matchedFields = new List<string>();
        if (duplicate.Email == normalizedEmail)
        {
            matchedFields.Add("email");
        }

        if (duplicate.Phone == normalizedPhone)
        {
            matchedFields.Add("phone");
        }

        if (!string.IsNullOrWhiteSpace(normalizedCnpj) && duplicate.Cnpj == normalizedCnpj)
        {
            matchedFields.Add("cnpj");
        }

        return new DuplicateLeadMatch(duplicate.ToDomain(), matchedFields);
    }

    public async Task<PagedResult<Lead>> ListAsync(ListLeadsQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var queryable = _dbContext.Leads.AsNoTracking().AsQueryable();

        if (query.Status.HasValue)
        {
            queryable = queryable.Where(lead => lead.Status == (int)query.Status.Value);
        }

        if (query.Temperature.HasValue)
        {
            queryable = queryable.Where(lead => lead.Temperature == (int)query.Temperature.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Region))
        {
            var region = query.Region.Trim();
            queryable = queryable.Where(lead => lead.Region == region);
        }

        if (!string.IsNullOrWhiteSpace(query.LeadType))
        {
            var leadType = query.LeadType.Trim();
            queryable = queryable.Where(lead => lead.LeadType == leadType);
        }

        if (!string.IsNullOrWhiteSpace(query.ProductInterest))
        {
            var productInterest = query.ProductInterest.Trim();
            queryable = queryable.Where(lead => lead.ProductInterest == productInterest);
        }

        if (!string.IsNullOrWhiteSpace(query.AssignedTo))
        {
            var assignedTo = query.AssignedTo.Trim();
            queryable = queryable.Where(lead => lead.AssignedTo == assignedTo);
        }

        if (query.MinScore.HasValue)
        {
            queryable = queryable.Where(lead => lead.Score >= query.MinScore.Value);
        }

        if (query.MaxScore.HasValue)
        {
            queryable = queryable.Where(lead => lead.Score <= query.MaxScore.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            queryable = queryable.Where(lead =>
                lead.Name.ToLower().Contains(search)
                || lead.Email.ToLower().Contains(search)
                || lead.Company.ToLower().Contains(search));
        }

        if (query.CampaignId.HasValue)
        {
            queryable = queryable.Where(lead => lead.CampaignId == query.CampaignId.Value);
        }

        var totalItems = await queryable.CountAsync(cancellationToken);
        var records = await queryable
            .OrderByDescending(lead => lead.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Lead>(
            records.Select(lead => lead.ToDomain()).ToArray(),
            query.Page,
            query.PageSize,
            totalItems);
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

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Lead id is required.", nameof(id));
        }

        var existingRecord = await _dbContext.Leads
            .FirstOrDefaultAsync(existingLead => existingLead.Id == id, cancellationToken);
        if (existingRecord is null)
        {
            return false;
        }

        _dbContext.Leads.Remove(existingRecord);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string NormalizeDigits(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value.Where(char.IsDigit).ToArray());
    }
}
