namespace InventoryAlert.Contracts.Entities;

public enum StockTransactionType
{
    Sale,
    Restock,
    Adjustment,
    Return
}

public class StockTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public StockTransactionType Type { get; set; }
    public int Quantity { get; set; }
    public string? Reference { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
