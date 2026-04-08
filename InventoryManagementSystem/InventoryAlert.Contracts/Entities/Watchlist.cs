namespace InventoryAlert.Contracts.Entities;

public class Watchlist
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Product? Product { get; set; }
}
