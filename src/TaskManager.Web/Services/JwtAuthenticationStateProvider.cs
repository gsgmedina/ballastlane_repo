using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace TaskManager.Web.Services;

/// <summary>
/// Supplies the app's authentication state from the JWT held in localStorage, and lets the
/// login/logout flows notify Blazor when that state changes.
/// </summary>
public sealed class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private const string AuthType = "jwt";
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly TokenStore _tokenStore;

    public JwtAuthenticationStateProvider(TokenStore tokenStore) => _tokenStore = tokenStore;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _tokenStore.GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            return Anonymous;

        var claims = JwtParser.ParseClaims(token);
        var expiry = JwtParser.GetExpiry(claims);
        if (expiry is not null && expiry <= DateTime.UtcNow)
        {
            await _tokenStore.ClearTokenAsync();
            return Anonymous;
        }

        var identity = new ClaimsIdentity(claims, AuthType, nameType: "name", roleType: ClaimTypes.Role);
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task NotifyLoginAsync(string token)
    {
        await _tokenStore.SetTokenAsync(token);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task NotifyLogoutAsync()
    {
        await _tokenStore.ClearTokenAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }
}
