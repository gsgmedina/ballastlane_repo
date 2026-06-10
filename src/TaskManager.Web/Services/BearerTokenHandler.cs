using System.Net.Http.Headers;

namespace TaskManager.Web.Services;

/// <summary>Attaches the stored JWT as a Bearer token to outgoing API requests.</summary>
public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly TokenStore _tokenStore;

    public BearerTokenHandler(TokenStore tokenStore) => _tokenStore = tokenStore;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenStore.GetTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}
