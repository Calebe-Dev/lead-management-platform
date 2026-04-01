namespace LeadManager.Application.Abstractions;

public interface IUserPasswordRepository
{
    Task<string?> GetPasswordHashByUsernameAsync(string username, CancellationToken cancellationToken = default);
}
