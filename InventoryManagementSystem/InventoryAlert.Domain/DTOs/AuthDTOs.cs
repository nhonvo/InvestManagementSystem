namespace InventoryAlert.Domain.DTOs;

public record LoginRequest(string Username, string Password);

public record RegisterRequest(string Username, string Password, string Email);

public record AuthResponse(
    string AccessToken,
    DateTime ExpiresAt);

/// <summary>
/// Internal token pair used to set the httpOnly refresh cookie while keeping API response bodies minimal.
/// </summary>
public record AuthTokenPair(
    AuthResponse Auth,
    string RefreshToken,
    DateTime RefreshExpiresAt);

public record RegistrationResponse(string Message, string Username);

/// <summary>
/// No body needed; refresh token is read from httpOnly cookie.
/// </summary>
public record RefreshRequest();
