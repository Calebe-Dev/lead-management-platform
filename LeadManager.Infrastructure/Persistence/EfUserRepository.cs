using LeadManager.Application.Abstractions;
using LeadManager.Application.Users;
using Microsoft.EntityFrameworkCore;

namespace LeadManager.Infrastructure.Persistence;

public sealed class EfUserRepository : IUserRepository, IUserPasswordRepository
{
    private readonly LeadManagerDbContext _dbContext;

    public EfUserRepository(LeadManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserResponse?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = (username ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Username == normalized, cancellationToken);
        return user?.ToResponse();
    }

    public async Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return user?.ToResponse();
    }

    public async Task<IReadOnlyCollection<UserResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .OrderBy(x => x.Username)
            .ToListAsync(cancellationToken);
        return users.Select(x => x.ToResponse()).ToArray();
    }

    public async Task<UserResponse> CreateAsync(CreateUserCommand command, string passwordHash, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var record = new UserRecord
        {
            Id = Guid.NewGuid(),
            Username = command.Username.Trim(),
            PasswordHash = passwordHash,
            Role = command.Role.Trim().ToLowerInvariant(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.Users.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return record.ToResponse();
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (record is null)
        {
            return false;
        }

        _dbContext.Users.Remove(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<string?> GetPasswordHashByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = (username ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.Username == normalized)
            .Select(x => new { x.PasswordHash })
            .FirstOrDefaultAsync(cancellationToken);
        return user?.PasswordHash;
    }
}

internal static class UserRecordMappings
{
    public static UserResponse ToResponse(this UserRecord user) =>
        new(
            user.Id,
            user.Username,
            user.Role,
            user.CreatedAtUtc,
            user.UpdatedAtUtc);
}
