namespace LeadManager.Application.Users;

public sealed record CreateUserCommand(string Username, string Password, string Role);
