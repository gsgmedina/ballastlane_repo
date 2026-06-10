using TaskManager.Application.Auth.Dtos;

namespace TaskManager.Application.Auth;

/// <summary>Business logic for user registration, login and profile retrieval.</summary>
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<UserResponse> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}
