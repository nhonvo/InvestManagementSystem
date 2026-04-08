using InventoryAlert.Api.Application.Common.Exceptions;
using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Domain.Constants;
using InventoryAlert.Api.Domain.Interfaces;
using InventoryAlert.Api.Web.Configuration;
using InventoryAlert.Contracts.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace InventoryAlert.Api.Application.Services;

public class AuthService(IUnitOfWork unitOfWork, AppSettings settings) : IAuthService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly AppSettings _settings = settings;

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var validUsername = _settings.Auth.Username;
        var validPassword = _settings.Auth.Password;

        if (string.IsNullOrEmpty(validUsername) || string.IsNullOrEmpty(validPassword))
        {
            throw new UserFriendlyException(ErrorCode.Internal, ApplicationConstants.Messages.AuthConfigMissing);
        }

        // 1. Check fixed admin credentials
        if (request.Username == validUsername && request.Password == validPassword)
        {
            var token = GenerateJwtToken(request.Username);
            return new AuthResponse(token);
        }

        // 2. Check DB users
        var user = await _unitOfWork.Users.GetByUsernameAsync(request.Username, ct);
        if (user != null)
        {
            if (BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                var token = GenerateJwtToken(user.Username);
                return new AuthResponse(token);
            }
        }

        throw new UserFriendlyException(ErrorCode.Unauthorized, ApplicationConstants.Messages.InvalidCredentials);
    }

    public async Task<RegistrationResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        // Check if user exists
        var exists = await _unitOfWork.Users.ExistsAsync(request.Username, ct);
        if (exists)
        {
            throw new UserFriendlyException(ErrorCode.Conflict, $"Username '{request.Username}' is already taken.");
        }

        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            await _unitOfWork.Users.AddAsync(user, ct);
        }, ct);

        return new RegistrationResponse("Registration successful");
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
