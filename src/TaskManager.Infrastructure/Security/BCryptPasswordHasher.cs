using TaskManager.Application.Abstractions;

namespace TaskManager.Infrastructure.Security;

/// <summary>Password hashing backed by BCrypt (adaptive work factor, per-hash salt).</summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // A stored value that isn't a valid BCrypt hash should never authenticate.
            return false;
        }
    }
}
