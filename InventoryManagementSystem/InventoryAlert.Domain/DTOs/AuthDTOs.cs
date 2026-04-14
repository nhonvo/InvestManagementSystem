namespace InventoryAlert.Domain.DTOs;

public record LoginRequest(string Username, string Password);

public record RegisterRequest(string Username, string Password, string Email);

public record AuthResponse(
    string AccessToken,
    DateTime ExpiresAt);

public record RegistrationResponse(string Message, string Username);

/// <summary>
/// No body needed; refresh token is read from httpOnly cookie.
/// </summary>
public record RefreshRequest();
