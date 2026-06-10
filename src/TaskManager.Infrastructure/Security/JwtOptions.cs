namespace TaskManager.Infrastructure.Security;

/// <summary>Configuration for issuing and validating JWT access tokens (bound from the "Jwt" section).</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "TaskManager.Api";
    public string Audience { get; set; } = "TaskManager.Web";

    /// <summary>Symmetric signing key. Must be at least 32 bytes (256 bits) for HS256.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 120;
}
