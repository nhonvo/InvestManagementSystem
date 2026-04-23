using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InventoryAlert.Api.Configuration;
using InventoryAlert.Domain.Common.Constants;
using InventoryAlert.Domain.Common.Exceptions;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace InventoryAlert.Api.Services;

public class AuthService(IUnitOfWork unitOfWork, ApiSettings settings) : IAuthService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ApiSettings _settings = settings;

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _unitOfWork.Users.GetByUsernameAsync(request.Username, ct);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UserFriendlyException(ErrorCode.Unauthorized, ApplicationConstants.Messages.InvalidCredentials);
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.Jwt.ExpiryMinutes > 0 ? _settings.Jwt.ExpiryMinutes : 60);
        var token = GenerateJwtToken(user, expiresAt);

        return new AuthResponse(token, expiresAt);
    }

    public async Task<RegistrationResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
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
                Role = "User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            await _unitOfWork.Users.AddAsync(user, ct);
        }, ct);

        return new RegistrationResponse("Registration successful", request.Username);
    }

    public Task LogoutAsync(CancellationToken ct = default)
    {
        // Cookie clearing is handled at the controller layer (Response.Cookies.Delete).
        // Service-layer hook reserved for future refresh token revocation table.
        return Task.CompletedTask;
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtSettings = _settings.Jwt;
        var key = jwtSettings.Key ?? throw new UnauthorizedAccessException("JWT key not configured.");

        try
        {
            var principal = handler.ValidateToken(refreshToken, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = false // refresh tokens may be slightly expired — we re-issue access
            }, out _);

            var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            {
                var claims = string.Join(", ", principal.Claims.Select(c => $"{c.Type}={c.Value}"));
                throw new UnauthorizedAccessException($"Invalid token subject. Claims found: {claims}");
            }

            var user = await _unitOfWork.Users.GetByIdAsync(userId, ct)
                ?? throw new UnauthorizedAccessException("User no longer exists.");

            var expiresAt = DateTime.UtcNow.AddMinutes(_settings.Jwt.ExpiryMinutes > 0 ? _settings.Jwt.ExpiryMinutes : 60);
            return new AuthResponse(GenerateJwtToken(user, expiresAt), expiresAt);
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException)
        {
            throw new UnauthorizedAccessException("Refresh token is invalid or expired.", ex);
        }
    }

    private string GenerateJwtToken(User user, DateTime expiresAt)
    {
        var jwtSettings = _settings.Jwt;
        var key = jwtSettings.Key ?? throw new UserFriendlyException(ErrorCode.Internal, ApplicationConstants.Messages.JwtKeyMissing);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            jwtSettings.Issuer,
            jwtSettings.Audience,
            claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
