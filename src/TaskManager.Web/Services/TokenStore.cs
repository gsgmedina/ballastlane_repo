using Microsoft.JSInterop;

namespace TaskManager.Web.Services;

/// <summary>Persists the JWT in browser localStorage via JS interop (no external dependency).</summary>
public sealed class TokenStore
{
    private const string Key = "taskmanager.authToken";
    private readonly IJSRuntime _js;

    public TokenStore(IJSRuntime js) => _js = js;

    public ValueTask<string?> GetTokenAsync()
        => _js.InvokeAsync<string?>("localStorage.getItem", Key);

    public ValueTask SetTokenAsync(string token)
        => _js.InvokeVoidAsync("localStorage.setItem", Key, token);

    public ValueTask ClearTokenAsync()
        => _js.InvokeVoidAsync("localStorage.removeItem", Key);
}
