using LeadManager.Application.Abstractions;

namespace LeadManager.Application.Auth;

public sealed class AuthUseCase
{
    private static readonly TimeSpan RefreshTokenTtl = TimeSpan.FromDays(14);
    private readonly IUserRepository _userRepository;
    private readonly IUserPasswordRepository _userPasswordRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public AuthUseCase(
        IUserRepository userRepository,
        IUserPasswordRepository userPasswordRepository,
        IPasswordHasher passwordHasher,
        ITokenIssuer tokenIssuer,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userRepository = userRepository;
        _userPasswordRepository = userPasswordRepository;
        _passwordHasher = passwordHasher;
        _tokenIssuer = tokenIssuer;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<TokenResponse?> IssueTokenAsync(TokenRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var username = request.Username?.Trim() ?? string.Empty;
        var password = request.Password?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var user = await _userRepository.GetByUsernameAsync(username, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var passwordHash = await _userPasswordRepository.GetPasswordHashByUsernameAsync(username, cancellationToken);
        if (string.IsNullOrWhiteSpace(passwordHash) || !_passwordHasher.Verify(passwordHash, password))
        {
            return null;
        }

        var accessToken = _tokenIssuer.IssueAccessToken(user.Id, user.Username, user.Role);
        var refreshToken = _tokenIssuer.GenerateRefreshToken();
        var refreshExpiresAt = DateTime.UtcNow.Add(RefreshTokenTtl);
        await _refreshTokenRepository.StoreAsync(
            new StoredRefreshToken(
                refreshToken,
                user.Id,
                user.Username,
                user.Role,
                refreshExpiresAt,
                DateTime.UtcNow,
                null),
            cancellationToken);

        return new TokenResponse(
            accessToken.AccessToken,
            accessToken.ExpiresAtUtc,
            refreshToken,
            refreshExpiresAt);
    }

    public async Task<TokenResponse?> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var refreshToken = request.RefreshToken?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var stored = await _refreshTokenRepository.GetAsync(refreshToken, cancellationToken);
        if (stored is null || stored.RevokedAtUtc.HasValue || stored.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return null;
        }

        await _refreshTokenRepository.RevokeAsync(refreshToken, DateTime.UtcNow, cancellationToken);

        var accessToken = _tokenIssuer.IssueAccessToken(stored.UserId, stored.Username, stored.Role);
        var newRefreshToken = _tokenIssuer.GenerateRefreshToken();
        var refreshExpiresAt = DateTime.UtcNow.Add(RefreshTokenTtl);
        await _refreshTokenRepository.StoreAsync(
            new StoredRefreshToken(
                newRefreshToken,
                stored.UserId,
                stored.Username,
                stored.Role,
                refreshExpiresAt,
                DateTime.UtcNow,
                null),
            cancellationToken);

        return new TokenResponse(
            accessToken.AccessToken,
            accessToken.ExpiresAtUtc,
            newRefreshToken,
            refreshExpiresAt);
    }

    public Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var refreshToken = request.RefreshToken?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Task.CompletedTask;
        }

        return _refreshTokenRepository.RevokeAsync(refreshToken, DateTime.UtcNow, cancellationToken);
    }
}
