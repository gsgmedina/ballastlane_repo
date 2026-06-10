using System.Security.Claims;
using System.Text.Json;

namespace TaskManager.Web.Services;

/// <summary>Decodes the (unverified) claims from a JWT payload for display/UX purposes only.</summary>
internal static class JwtParser
{
    public static IReadOnlyList<Claim> ParseClaims(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2)
            return Array.Empty<Claim>();

        var json = Decode(parts[1]);
        var map = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        if (map is null)
            return Array.Empty<Claim>();

        var claims = new List<Claim>();
        foreach (var (key, value) in map)
        {
            if (value.ValueKind == JsonValueKind.Array)
                claims.AddRange(value.EnumerateArray().Select(e => new Claim(key, e.ToString())));
            else
                claims.Add(new Claim(key, value.ToString()));
        }
        return claims;
    }

    /// <summary>Returns the token's expiry (from the "exp" claim) in UTC, if present.</summary>
    public static DateTime? GetExpiry(IEnumerable<Claim> claims)
    {
        var exp = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        return long.TryParse(exp, out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime
            : null;
    }

    private static string Decode(string base64Url)
    {
        var padded = base64Url.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(padded));
    }
}
