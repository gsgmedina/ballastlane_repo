using TaskManager.Domain.Common;

namespace TaskManager.Domain.Entities;

/// <summary>
/// An application user. Authentication credentials are stored as a one-way hash only.
/// </summary>
public sealed class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string DisplayName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private User(Guid id, string email, string passwordHash, string displayName, DateTime createdAtUtc)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        DisplayName = displayName;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a brand-new user. Validates required invariants.</summary>
    public static User Create(string email, string passwordHash, string displayName, DateTime nowUtc)
    {
        email = (email ?? string.Empty).Trim();
        displayName = (displayName ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash is required.");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Display name is required.");

        return new User(Guid.NewGuid(), email.ToLowerInvariant(), passwordHash, displayName, nowUtc);
    }

    /// <summary>Rehydrates a user from persistence without re-running creation guards.</summary>
    public static User FromPersistence(Guid id, string email, string passwordHash, string displayName, DateTime createdAtUtc)
        => new(id, email, passwordHash, displayName, createdAtUtc);
}
