namespace LeadManager.Application.Auth;

public sealed record AccessTokenResult(string AccessToken, DateTime ExpiresAtUtc);
