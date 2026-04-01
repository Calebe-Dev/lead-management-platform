using LeadManager.Application.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace LeadManager.Infrastructure.Auth;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Password value is required.", nameof(value));
        }

        var salt = RandomNumberGenerator.GetBytes(16);
        var payload = ComputeHash(value, salt);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(payload)}";
    }

    public bool Verify(string hashedValue, string plainValue)
    {
        if (string.IsNullOrWhiteSpace(hashedValue) || string.IsNullOrWhiteSpace(plainValue))
        {
            return false;
        }

        var parts = hashedValue.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        if (!TryDecode(parts[0], out var salt) || !TryDecode(parts[1], out var expected))
        {
            return false;
        }

        var computed = ComputeHash(plainValue, salt);
        return CryptographicOperations.FixedTimeEquals(expected, computed);
    }

    private static bool TryDecode(string value, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromBase64String(value);
            return true;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }

    private static byte[] ComputeHash(string value, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(value), salt, 100_000, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(32);
    }
}
