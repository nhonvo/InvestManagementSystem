using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
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

    private static CookieOptions BuildRefreshCookieOptions(HttpContext httpContext, DateTime expiresAt)
    {
        // Important: SameSite=None cookies must also be Secure, otherwise modern browsers will reject them.
        // For localhost/dev, browsers allow Secure cookies over HTTP.
        var isHttps = httpContext.Request.IsHttps;
        var isLocal = httpContext.Request.Host.Host == "localhost" || httpContext.Request.Host.Host == "127.0.0.1";

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps, // Only secure if actually HTTPS
            SameSite = (isHttps || isLocal) ? SameSiteMode.None : SameSiteMode.Lax,
            Path = "/",
            Expires = expiresAt
        };
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var tokens = await _authService.LoginAsync(request, ct);
        Response.Cookies.Append(
            RefreshTokenCookie,
            tokens.RefreshToken,
            BuildRefreshCookieOptions(HttpContext, tokens.RefreshExpiresAt));

        return Ok(tokens.Auth);
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<RegistrationResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var res = await _authService.RegisterAsync(request, ct);
        return Ok(res);
    }

    /// <summary>
    /// Exchange a valid httpOnly refresh token cookie for a new access JWT.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken ct)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookie];
        if (string.IsNullOrEmpty(refreshToken))
        {
            // Fallback for testing purposes
            if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var header = authHeader.ToString();
                if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    refreshToken = header.Substring("Bearer ".Length).Trim();
                }
            }
        }
        
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { Message = "Refresh token is missing." });

        var tokens = await _authService.RefreshAsync(refreshToken, ct);
        Response.Cookies.Append(
            RefreshTokenCookie,
            tokens.RefreshToken,
            BuildRefreshCookieOptions(HttpContext, tokens.RefreshExpiresAt));

        return Ok(tokens.Auth);
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
