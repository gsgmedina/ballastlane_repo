namespace TaskManager.Application.Abstractions;

/// <summary>One-way password hashing. Implemented in Infrastructure (BCrypt).</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
