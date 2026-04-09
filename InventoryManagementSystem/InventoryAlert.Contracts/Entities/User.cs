namespace InventoryAlert.Contracts.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; // In a real app, use BCrypt/Argon2
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "User"; // Default role
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
