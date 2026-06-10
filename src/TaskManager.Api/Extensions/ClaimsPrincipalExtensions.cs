using System.Security.Claims;

namespace TaskManager.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Returns the authenticated user's id from the token's NameIdentifier/sub claim.</summary>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? principal.FindFirstValue("sub");

        if (Guid.TryParse(value, out var id))
            return id;

        throw new InvalidOperationException("The authenticated principal does not carry a valid user id.");
    }
}
