namespace LeadManager.Application.Auth;

public sealed record StoredRefreshToken(
    string Token,
    Guid UserId,
    string Username,
    string Role,
    DateTime ExpiresAtUtc,
    DateTime CreatedAtUtc,
    DateTime? RevokedAtUtc);
