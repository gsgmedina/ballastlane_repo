using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Extensions;
using TaskManager.Application.Auth;
using TaskManager.Application.Auth.Dtos;

namespace TaskManager.Api.Controllers;

/// <summary>
/// The "second API": user creation, login, plus an authorized and a non-authorized endpoint.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Registers a new user and returns a bearer token. (Non-authorized)</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(request, ct);
        return CreatedAtAction(nameof(Me), new { }, result);
    }

    /// <summary>Authenticates a user and returns a bearer token. (Non-authorized)</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
        => Ok(await _auth.LoginAsync(request, ct));

    /// <summary>Returns the current user's profile. (Authorized)</summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken ct)
        => Ok(await _auth.GetCurrentUserAsync(User.GetUserId(), ct));

    /// <summary>A public health/demo endpoint requiring no authentication. (Non-authorized)</summary>
    [AllowAnonymous]
    [HttpGet("ping")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> Ping() => Ok(new { status = "ok", service = "TaskManager.Auth" });
}
