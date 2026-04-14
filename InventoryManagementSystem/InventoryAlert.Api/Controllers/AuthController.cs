using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAlert.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController(IAuthService authService, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    private readonly IAuthService _authService = authService;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    private const string RefreshTokenCookie = "refreshToken";

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var res = await _authService.LoginAsync(request, ct);
        // In a full production system, set the refresh token here as an httpOnly cookie:
        // Response.Cookies.Append(RefreshTokenCookie, refreshToken, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });
        return Ok(res);
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegistrationResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var res = await _authService.RegisterAsync(request, ct);
        return Ok(res);
    }

    /// <summary>
    /// Exchange a valid httpOnly refresh token cookie for a new access JWT.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken ct)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookie];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { Message = "Refresh token is missing." });

        var res = await _authService.RefreshAsync(refreshToken, ct);
        return Ok(res);
    }

    /// <summary>
    /// Revoke the current session. Clears the httpOnly refresh token cookie.
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await _authService.LogoutAsync(ct);
        Response.Cookies.Delete(RefreshTokenCookie);
        return Ok(new { Message = "Logged out." });
    }
}
