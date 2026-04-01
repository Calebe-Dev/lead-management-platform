using LeadManager.Application.Auth;

namespace LeadManager.Application.Abstractions;

public interface IRefreshTokenRepository
{
    Task<StoredRefreshToken?> GetAsync(string token, CancellationToken cancellationToken = default);
    Task StoreAsync(StoredRefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAsync(string token, DateTime revokedAtUtc, CancellationToken cancellationToken = default);
    Task RevokeByUserAsync(Guid userId, DateTime revokedAtUtc, CancellationToken cancellationToken = default);
}
