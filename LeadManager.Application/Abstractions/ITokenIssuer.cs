using LeadManager.Application.Auth;

namespace LeadManager.Application.Abstractions;

public interface ITokenIssuer
{
    AccessTokenResult IssueAccessToken(Guid userId, string username, string role);
    string GenerateRefreshToken();
}
