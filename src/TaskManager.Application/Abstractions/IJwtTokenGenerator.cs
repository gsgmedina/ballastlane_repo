using TaskManager.Domain.Entities;

namespace TaskManager.Application.Abstractions;

/// <summary>Issues signed JWT access tokens for authenticated users.</summary>
public interface IJwtTokenGenerator
{
    AuthToken Generate(User user);
}

/// <summary>A signed access token and its expiry.</summary>
public sealed record AuthToken(string Token, DateTime ExpiresAtUtc);
