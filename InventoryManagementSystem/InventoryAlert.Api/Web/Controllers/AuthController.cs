using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InventoryAlert.Api.Application.Common.Exceptions;
using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Domain.Constants;
using InventoryAlert.Api.Web.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace InventoryAlert.Api.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AppSettings settings) : ControllerBase
{
    private readonly AppSettings _settings = settings;

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var validUsername = _settings.Auth.Username;
        var validPassword = _settings.Auth.Password;

        if (string.IsNullOrEmpty(validUsername) || string.IsNullOrEmpty(validPassword))
        {
            throw new UserFriendlyException(ErrorCode.Internal, ApplicationConstants.Messages.AuthConfigMissing);
        }

        if (request.Username == validUsername && request.Password == validPassword)
        {
            var token = GenerateJwtToken(request.Username);
            return Ok(new { token });
        }

        throw new UserFriendlyException(ErrorCode.Unauthorized, ApplicationConstants.Messages.InvalidCredentials);
    }

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        // The ValidateModelFilter will automatically handle FluentValidation errors for RegisterRequest

        // TODO: Implement user creation logic
        return Ok(new { message = "Registration successful" });
    }

    private string GenerateJwtToken(string username)
    {
        var jwtSettings = _settings.Jwt;
        var key = jwtSettings.Key ?? throw new UserFriendlyException(ErrorCode.Internal, ApplicationConstants.Messages.JwtKeyMissing);
        var issuer = jwtSettings.Issuer;
        var audience = jwtSettings.Audience;

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
