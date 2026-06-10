namespace TaskManager.Web.Models;

public sealed record RegisterRequest(string Email, string Password, string DisplayName);

public sealed record LoginRequest(string Email, string Password);

public sealed record UserResponse(Guid Id, string Email, string DisplayName);

public sealed record AuthResponse(string Token, DateTime ExpiresAtUtc, UserResponse User);

/// <summary>RFC 7807 problem details returned by the API on errors.</summary>
public sealed record ProblemResponse(
    string? Title,
    int? Status,
    string? Detail,
    Dictionary<string, string[]>? Errors);
