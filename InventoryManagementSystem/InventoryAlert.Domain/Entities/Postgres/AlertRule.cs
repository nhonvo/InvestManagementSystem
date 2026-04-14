namespace InventoryAlert.Domain.Entities.Postgres;

public enum AlertCondition
{
    /// <summary>
    /// Trigger when current price exceeds TargetValue.
    /// </summary>
    PriceAbove,

    /// <summary>
    /// Trigger when current price falls below TargetValue.
    /// </summary>
    PriceBelow,

    /// <summary>
    /// Trigger when price hits an exact target (±tolerance).
    /// </summary>
    PriceTargetReached,

    /// <summary>
    /// Trigger when unrealized loss % exceeds TargetValue.
    /// </summary>
    PercentDropFromCost,

    /// <summary>
    /// Trigger when user's share count drops below TargetValue.
    /// </summary>
    LowHoldingsCount
}

/// <summary>
/// Unified Notification Rules. Triggers on both price and inventory conditions.
/// </summary>
public class AlertRule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// FK -> User.Id
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// FK -> StockListing.TickerSymbol
    /// </summary>
    public string TickerSymbol { get; set; } = string.Empty;

    public AlertCondition Condition { get; set; }

    /// <summary>
    /// Threshold to breach.
    /// </summary>
    public decimal TargetValue { get; set; }

    /// <summary>
    /// Enabled/disabled state.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Deactivate automatically after first trigger.
    /// </summary>
    public bool TriggerOnce { get; set; } = true;

    /// <summary>
    /// UTC timestamp of last successful trigger.
    /// </summary>
    public DateTime? LastTriggeredAt { get; set; }

    /// <summary>
    /// Rule creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
