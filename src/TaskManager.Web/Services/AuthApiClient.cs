using System.Net.Http.Json;
using TaskManager.Web.Models;

namespace TaskManager.Web.Services;

/// <summary>Typed client for the Auth API plus login/register orchestration with the auth provider.</summary>
public sealed class AuthApiClient
{
    private readonly HttpClient _http;
    private readonly JwtAuthenticationStateProvider _authState;

    public AuthApiClient(HttpClient http, JwtAuthenticationStateProvider authState)
    {
        _http = http;
        _authState = authState;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", request, ct);
        var auth = await ReadAuthOrThrowAsync(response);
        await _authState.NotifyLoginAsync(auth.Token);
        return auth;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", request, ct);
        var auth = await ReadAuthOrThrowAsync(response);
        await _authState.NotifyLoginAsync(auth.Token);
        return auth;
    }

    public Task LogoutAsync() => _authState.NotifyLogoutAsync();

    private static async Task<AuthResponse> ReadAuthOrThrowAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;

        ProblemResponse? problem = null;
        try { problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(); }
        catch { /* non-JSON body */ }

        throw ApiException.From(response.StatusCode, problem);
    }
}
