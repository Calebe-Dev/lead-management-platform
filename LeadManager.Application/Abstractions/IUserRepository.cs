using LeadManager.Application.Users;

namespace LeadManager.Application.Abstractions;

public interface IUserRepository
{
    Task<UserResponse?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<UserResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<UserResponse> CreateAsync(CreateUserCommand command, string passwordHash, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
