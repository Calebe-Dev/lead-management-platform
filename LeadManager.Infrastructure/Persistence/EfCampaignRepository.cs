using LeadManager.Application.Abstractions;
using LeadManager.Application.Campaigns;
using Microsoft.EntityFrameworkCore;

namespace LeadManager.Infrastructure.Persistence;

public sealed class EfCampaignRepository : ICampaignRepository
{
    private readonly LeadManagerDbContext _dbContext;

    public EfCampaignRepository(LeadManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CampaignResponse> CreateAsync(UpsertCampaignCommand command, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var record = new CampaignRecord
        {
            Id = Guid.NewGuid(),
            Name = command.Name.Trim(),
            Channel = command.Channel.Trim(),
            Utm = command.Utm?.Trim() ?? string.Empty,
            IsActive = command.IsActive,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.Campaigns.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return record.ToResponse();
    }

    public async Task<CampaignResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Campaigns
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return record?.ToResponse();
    }

    public async Task<IReadOnlyCollection<CampaignResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.Campaigns
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        return records.Select(x => x.ToResponse()).ToArray();
    }

    public async Task<CampaignResponse?> UpdateAsync(Guid id, UpsertCampaignCommand command, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Campaigns
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (record is null)
        {
            return null;
        }

        record.Name = command.Name.Trim();
        record.Channel = command.Channel.Trim();
        record.Utm = command.Utm?.Trim() ?? string.Empty;
        record.IsActive = command.IsActive;
        record.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return record.ToResponse();
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Campaigns
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (record is null)
        {
            return false;
        }

        _dbContext.Campaigns.Remove(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

internal static class CampaignRecordMappings
{
    public static CampaignResponse ToResponse(this CampaignRecord record) =>
        new(
            record.Id,
            record.Name,
            record.Channel,
            record.Utm,
            record.IsActive,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);
}
