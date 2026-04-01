using LeadManager.Application.Abstractions;

namespace LeadManager.Application.Users;

public sealed class UserManagementUseCase
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin",
        "marketing",
        "vendas"
    };

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public UserManagementUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public Task<IReadOnlyCollection<UserResponse>> ListAsync(CancellationToken cancellationToken = default) =>
        _userRepository.ListAsync(cancellationToken);

    public async Task<UserResponse> CreateAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var username = command.Username?.Trim() ?? string.Empty;
        var password = command.Password?.Trim() ?? string.Empty;
        var role = command.Role?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username is required.", nameof(command.Username));
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new ArgumentException("Password must have at least 8 characters.", nameof(command.Password));
        }

        if (!AllowedRoles.Contains(role))
        {
            throw new ArgumentException("Role is invalid. Allowed values: admin, marketing, vendas.", nameof(command.Role));
        }

        var existing = await _userRepository.GetByUsernameAsync(username, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"User '{username}' already exists.");
        }

        var hash = _passwordHasher.Hash(password);
        return await _userRepository.CreateAsync(new CreateUserCommand(username, string.Empty, role), hash, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(id));
        }

        await _refreshTokenRepository.RevokeByUserAsync(id, DateTime.UtcNow, cancellationToken);
        return await _userRepository.DeleteAsync(id, cancellationToken);
    }
}
