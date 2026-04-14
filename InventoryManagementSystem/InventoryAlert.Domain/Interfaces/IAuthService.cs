using InventoryAlert.Domain.DTOs;

namespace InventoryAlert.Domain.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<RegistrationResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    /// <summary>Revoke the active session. Clears httpOnly refresh cookie server-side.</summary>
    Task LogoutAsync(CancellationToken ct = default);
    /// <summary>Issue a new access token, reading the refresh token from the httpOnly cookie.</summary>
    Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct = default);
}
