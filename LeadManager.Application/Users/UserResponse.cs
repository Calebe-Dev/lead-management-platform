namespace LeadManager.Application.Users;

public sealed record UserResponse(
    Guid Id,
    string Username,
    string Role,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
