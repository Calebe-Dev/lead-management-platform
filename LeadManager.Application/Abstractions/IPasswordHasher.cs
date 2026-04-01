namespace LeadManager.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string value);
    bool Verify(string hashedValue, string plainValue);
}
