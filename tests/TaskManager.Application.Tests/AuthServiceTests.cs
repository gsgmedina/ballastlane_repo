using FluentAssertions;
using FluentValidation;
using NSubstitute;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Auth;
using TaskManager.Application.Auth.Dtos;
using TaskManager.Application.Auth.Validators;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Tests.TestSupport;
using TaskManager.Domain.Entities;
using Xunit;

namespace TaskManager.Application.Tests;

public class AuthServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _tokens = Substitute.For<IJwtTokenGenerator>();
    private readonly FakeClock _clock = FakeClock.Default;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _tokens.Generate(Arg.Any<User>())
            .Returns(ci => new AuthToken("jwt-token", _clock.UtcNow.AddHours(1)));

        _sut = new AuthService(
            _users, _hasher, _tokens, _clock,
            new RegisterRequestValidator(), new LoginRequestValidator());
    }

    [Fact]
    public async Task Register_NewUser_HashesPasswordPersistsAndReturnsToken()
    {
        _hasher.Hash("Password1").Returns("hashed");
        _users.ExistsByEmailAsync("new@example.com", Arg.Any<CancellationToken>()).Returns(false);
        var req = new RegisterRequest("New@Example.com", "Password1", "New User");

        var result = await _sut.RegisterAsync(req);

        result.Token.Should().Be("jwt-token");
        result.User.Email.Should().Be("new@example.com");
        await _users.Received(1).AddAsync(
            Arg.Is<User>(u => u.Email == "new@example.com" && u.PasswordHash == "hashed"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_DuplicateEmail_ThrowsConflict()
    {
        _users.ExistsByEmailAsync("dup@example.com", Arg.Any<CancellationToken>()).Returns(true);
        var req = new RegisterRequest("dup@example.com", "Password1", "Dup");

        var act = () => _sut.RegisterAsync(req);

        await act.Should().ThrowAsync<ConflictException>();
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("not-an-email", "Password1", "Name")]
    [InlineData("ok@example.com", "short", "Name")]      // too short / no digit
    [InlineData("ok@example.com", "Password1", "")]      // missing display name
    public async Task Register_InvalidInput_ThrowsValidation(string email, string pwd, string name)
    {
        var act = () => _sut.RegisterAsync(new RegisterRequest(email, pwd, name));
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        var user = User.Create("user@example.com", "stored-hash", "User", _clock.UtcNow);
        _users.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("Password1", "stored-hash").Returns(true);

        var result = await _sut.LoginAsync(new LoginRequest("User@Example.com", "Password1"));

        result.Token.Should().Be("jwt-token");
        result.User.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsInvalidCredentials()
    {
        var user = User.Create("user@example.com", "stored-hash", "User", _clock.UtcNow);
        _users.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var act = () => _sut.LoginAsync(new LoginRequest("user@example.com", "wrong"));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Login_UnknownEmail_ThrowsInvalidCredentials()
    {
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = () => _sut.LoginAsync(new LoginRequest("ghost@example.com", "Password1"));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task GetCurrentUser_WhenMissing_ThrowsNotFound()
    {
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        var act = () => _sut.GetCurrentUserAsync(Guid.NewGuid());
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
