namespace InventoryAlert.Api.Application.DTOs;

public record LoginRequest(string Username, string Password);

public record RegisterRequest(string Username, string Password, string Email);

public record AuthResponse(string Token);

public record RegistrationResponse(string Message); 
