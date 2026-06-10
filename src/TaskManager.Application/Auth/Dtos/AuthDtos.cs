using TaskManager.Domain.Entities;

namespace TaskManager.Application.Auth.Dtos;

/// <summary>Payload to register a new user.</summary>
public sealed record RegisterRequest(string Email, string Password, string DisplayName);

/// <summary>Payload to log in.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Public-safe representation of a user.</summary>
public sealed record UserResponse(Guid Id, string Email, string DisplayName)
{
    public static UserResponse FromEntity(User u) => new(u.Id, u.Email, u.DisplayName);
}

/// <summary>Result of a successful register/login: a bearer token plus the user profile.</summary>
public sealed record AuthResponse(string Token, DateTime ExpiresAtUtc, UserResponse User);
