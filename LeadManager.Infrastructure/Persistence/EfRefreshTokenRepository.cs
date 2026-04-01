using LeadManager.Application.Abstractions;
using LeadManager.Application.Auth;
using Microsoft.EntityFrameworkCore;

namespace LeadManager.Infrastructure.Persistence;

public sealed class EfRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly LeadManagerDbContext _dbContext;

    public EfRefreshTokenRepository(LeadManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StoredRefreshToken?> GetAsync(string token, CancellationToken cancellationToken = default)
    {
        var normalized = (token ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var record = await _dbContext.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Token == normalized, cancellationToken);
        return record?.ToModel();
    }

    public async Task StoreAsync(StoredRefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(refreshToken);
        var record = new RefreshTokenRecord
        {
            Id = Guid.NewGuid(),
            Token = refreshToken.Token,
            UserId = refreshToken.UserId,
            Username = refreshToken.Username,
            Role = refreshToken.Role,
            ExpiresAtUtc = refreshToken.ExpiresAtUtc,
            CreatedAtUtc = refreshToken.CreatedAtUtc,
            RevokedAtUtc = refreshToken.RevokedAtUtc
        };

        _dbContext.RefreshTokens.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAsync(string token, DateTime revokedAtUtc, CancellationToken cancellationToken = default)
    {
        var normalized = (token ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        var record = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == normalized, cancellationToken);
        if (record is null)
        {
            return;
        }

        record.RevokedAtUtc = revokedAtUtc;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeByUserAsync(Guid userId, DateTime revokedAtUtc, CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.RefreshTokens
            .Where(x => x.UserId == userId && !x.RevokedAtUtc.HasValue)
            .ToListAsync(cancellationToken);
        if (records.Count == 0)
        {
            return;
        }

        foreach (var record in records)
        {
            record.RevokedAtUtc = revokedAtUtc;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

internal static class RefreshTokenMappings
{
    public static StoredRefreshToken ToModel(this RefreshTokenRecord record) =>
        new(
            record.Token,
            record.UserId,
            record.Username,
            record.Role,
            record.ExpiresAtUtc,
            record.CreatedAtUtc,
            record.RevokedAtUtc);
}
