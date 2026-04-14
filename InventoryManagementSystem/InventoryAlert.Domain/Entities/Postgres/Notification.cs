namespace InventoryAlert.Domain.Entities.Postgres;

/// <summary>
/// In-App Alerts Delivery. Stores triggered alert messages for the authenticated user.
/// </summary>
public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// FK -> User.Id.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// FK -> AlertRule.Id. Nullable (for system messages).
    /// </summary>
    public Guid? AlertRuleId { get; set; }

    /// <summary>
    /// Symbol that triggered the alert.
    /// </summary>
    public string? TickerSymbol { get; set; }

    /// <summary>
    /// Human-readable alert message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Whether the user has acknowledged it.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// UTC timestamp of notification creation.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
