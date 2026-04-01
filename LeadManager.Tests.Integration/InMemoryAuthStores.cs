using LeadManager.Application.Abstractions;
using LeadManager.Application.Auth;
using LeadManager.Application.Users;

namespace LeadManager.Tests.Integration;

internal sealed class InMemoryUserRepository : IUserRepository, IUserPasswordRepository
{
    private readonly List<InMemoryUser> _users = [];
    private readonly Lock _sync = new();

    public Task<UserResponse?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = username?.Trim().ToLowerInvariant() ?? string.Empty;
        lock (_sync)
        {
            var user = _users.FirstOrDefault(x => x.Username.Equals(normalized, StringComparison.Ordinal));
            return Task.FromResult(user?.ToResponse());
        }
    }

    public Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var user = _users.FirstOrDefault(x => x.Id == id);
            return Task.FromResult(user?.ToResponse());
        }
    }

    public Task<IReadOnlyCollection<UserResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            IReadOnlyCollection<UserResponse> users = _users.Select(x => x.ToResponse()).ToArray();
            return Task.FromResult(users);
        }
    }

    public Task<UserResponse> CreateAsync(CreateUserCommand command, string passwordHash, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var user = new InMemoryUser(
            Guid.NewGuid(),
            command.Username.Trim().ToLowerInvariant(),
            passwordHash,
            command.Role.Trim().ToLowerInvariant(),
            now,
            now);
        lock (_sync)
        {
            _users.Add(user);
        }

        return Task.FromResult(user.ToResponse());
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var removed = _users.RemoveAll(x => x.Id == id) > 0;
            return Task.FromResult(removed);
        }
    }

    public Task<string?> GetPasswordHashByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = username?.Trim().ToLowerInvariant() ?? string.Empty;
        lock (_sync)
        {
            var passwordHash = _users.FirstOrDefault(x => x.Username.Equals(normalized, StringComparison.Ordinal))?.PasswordHash;
            return Task.FromResult(passwordHash);
        }
    }

    private sealed record InMemoryUser(
        Guid Id,
        string Username,
        string PasswordHash,
        string Role,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc)
    {
        public UserResponse ToResponse() => new(Id, Username, Role, CreatedAtUtc, UpdatedAtUtc);
    }
}

internal sealed class InMemoryRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly Dictionary<string, StoredRefreshToken> _tokens = new(StringComparer.Ordinal);
    private readonly Lock _sync = new();

    public Task<StoredRefreshToken?> GetAsync(string token, CancellationToken cancellationToken = default)
    {
        var normalized = token?.Trim() ?? string.Empty;
        lock (_sync)
        {
            _tokens.TryGetValue(normalized, out var refreshToken);
            return Task.FromResult(refreshToken);
        }
    }

    public Task StoreAsync(StoredRefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _tokens[refreshToken.Token] = refreshToken;
        }

        return Task.CompletedTask;
    }

    public Task RevokeAsync(string token, DateTime revokedAtUtc, CancellationToken cancellationToken = default)
    {
        var normalized = token?.Trim() ?? string.Empty;
        lock (_sync)
        {
            if (_tokens.TryGetValue(normalized, out var existing))
            {
                _tokens[normalized] = existing with { RevokedAtUtc = revokedAtUtc };
            }
        }

        return Task.CompletedTask;
    }

    public Task RevokeByUserAsync(Guid userId, DateTime revokedAtUtc, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var keys = _tokens.Where(x => x.Value.UserId == userId).Select(x => x.Key).ToArray();
            foreach (var key in keys)
            {
                _tokens[key] = _tokens[key] with { RevokedAtUtc = revokedAtUtc };
            }
        }

        return Task.CompletedTask;
    }
}
