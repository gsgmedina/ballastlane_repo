using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaskManager.Application.Auth.Dtos;
using TaskManager.Infrastructure.Persistence;
using Xunit;

namespace TaskManager.Api.Tests;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthEndpointsTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Ping_IsPublic_Returns200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/auth/ping");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_Then_Me_ReturnsProfile()
    {
        var (client, auth) = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var me = await response.ReadAsync<UserResponse>();
        me!.Id.Should().Be(auth.User.Id);
        me.Email.Should().Be(auth.User.Email);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        var email = $"dup-{Guid.NewGuid():N}@example.com";
        var req = new RegisterRequest(email, "Password1", "Dup User");

        (await client.PostAsJsonAsync("/api/auth/register", req, ApiTestExtensions.Json))
            .EnsureSuccessStatusCode();
        var second = await client.PostAsJsonAsync("/api/auth/register", req, ApiTestExtensions.Json);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_InvalidPassword_Returns400()
    {
        var client = _factory.CreateClient();
        var req = new RegisterRequest($"x-{Guid.NewGuid():N}@example.com", "weak", "Name");

        var response = await client.PostAsJsonAsync("/api/auth/register", req, ApiTestExtensions.Json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithSeededDemoCredentials_Succeeds()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(DataSeeder.DemoEmail, DataSeeder.DemoPassword), ApiTestExtensions.Json);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.ReadAsync<AuthResponse>();
        auth!.Token.Should().NotBeNullOrWhiteSpace();
        auth.User.Email.Should().Be(DataSeeder.DemoEmail);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(DataSeeder.DemoEmail, "WrongPassword1"), ApiTestExtensions.Json);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
