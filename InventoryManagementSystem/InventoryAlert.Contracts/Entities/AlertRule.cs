namespace InventoryAlert.Contracts.Entities;

public class AlertRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;       // price | volume | change_pct
    public string Operator { get; set; } = string.Empty;    // gt | lt | gte | lte | eq
    public decimal Threshold { get; set; }
    public string NotifyChannel { get; set; } = "telegram";
    public bool IsActive { get; set; } = true;
    public DateTime? LastTriggeredAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
