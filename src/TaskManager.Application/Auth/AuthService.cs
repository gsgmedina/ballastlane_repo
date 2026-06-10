using FluentValidation;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Auth.Dtos;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IClock _clock;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        IClock clock,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _clock = clock;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        await _registerValidator.ValidateAndThrowAsync(request, ct);

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _users.ExistsByEmailAsync(email, ct))
            throw new ConflictException($"An account with email '{email}' already exists.");

        var user = User.Create(email, _passwordHasher.Hash(request.Password), request.DisplayName, _clock.UtcNow);
        await _users.AddAsync(user, ct);

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        await _loginValidator.ValidateAndThrowAsync(request, ct);

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(email, ct);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException();

        return BuildAuthResponse(user);
    }

    public async Task<UserResponse> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null)
            throw NotFoundException.For("User", userId);

        return UserResponse.FromEntity(user);
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        var token = _tokenGenerator.Generate(user);
        return new AuthResponse(token.Token, token.ExpiresAtUtc, UserResponse.FromEntity(user));
    }
}
