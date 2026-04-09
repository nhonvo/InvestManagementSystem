using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace InventoryAlert.Api.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, [FromServices] IMemoryCache cache, CancellationToken ct)
    {
        var cacheKey = $"login_attempts_{request.Username}";
        if (cache.TryGetValue(cacheKey, out int attempts) && attempts >= 100)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, "Too many failed login attempts. Please try again in 15 minutes.");
        }

        try
        {
            var response = await _authService.LoginAsync(request, ct);
            cache.Remove(cacheKey);
            return Ok(response);
        }
        catch (InventoryAlert.Contracts.Common.Exceptions.UserFriendlyException ex) when (ex.ErrorCode == InventoryAlert.Contracts.Common.Exceptions.ErrorCode.Unauthorized)
        {
            attempts++;
            cache.Set(cacheKey, attempts, TimeSpan.FromMinutes(15));
            throw;
        }
    }

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var response = await _authService.RegisterAsync(request, ct);
        return Ok(response);
    }
}
