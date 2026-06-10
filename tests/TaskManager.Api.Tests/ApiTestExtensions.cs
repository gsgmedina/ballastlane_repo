using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskManager.Application.Auth.Dtos;

namespace TaskManager.Api.Tests;

internal static class ApiTestExtensions
{
    /// <summary>JSON options matching the API (enums serialized as names).</summary>
    public static readonly JsonSerializerOptions Json = CreateJson();

    private static JsonSerializerOptions CreateJson()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    public static Task<T?> ReadAsync<T>(this HttpResponseMessage response)
        => response.Content.ReadFromJsonAsync<T>(Json);

    /// <summary>Registers a new unique user and returns an HttpClient pre-authenticated as that user.</summary>
    public static async Task<(HttpClient client, AuthResponse auth)> CreateAuthenticatedClientAsync(
        this CustomWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        var email = $"user-{Guid.NewGuid():N}@example.com";
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "Password1", "Test User"), Json);
        response.EnsureSuccessStatusCode();

        var auth = (await response.ReadAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return (client, auth);
    }
}
