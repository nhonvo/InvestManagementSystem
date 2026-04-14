namespace InventoryAlert.Domain.Entities.Postgres;

/// <summary>
/// Personal Watchlist. Tracks user interest without ownership.
/// Composite key (UserId, TickerSymbol).
/// </summary>
public class WatchlistItem
{
    /// <summary>
    /// FK -> User.Id
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// FK -> StockListing.TickerSymbol
    /// </summary>
    public string TickerSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of entry.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
