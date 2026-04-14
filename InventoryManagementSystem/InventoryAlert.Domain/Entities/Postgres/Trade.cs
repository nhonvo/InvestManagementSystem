namespace InventoryAlert.Domain.Entities.Postgres;

public enum TradeType
{
    Buy,
    Sell,
    Dividend,
    Split
}

/// <summary>
/// Ownership Ledger. Immutable record of position changes.
/// </summary>
public class Trade
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

    public TradeType Type { get; set; }

    /// <summary>
    /// Always positive. Direction is encoded by Type.
    /// Net holdings = SUM(Buy) - SUM(Sell) computed by service.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Execution price per share. 0 for Dividend/Split.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// UTC execution timestamp.
    /// </summary>
    public DateTime TradedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional user annotation.
    /// </summary>
    public string? Notes { get; set; }
}
