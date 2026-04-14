namespace InventoryAlert.Domain.Entities.Postgres;

/// <summary>
/// Price Log. Point-in-time market price snapshots for charting and alert evaluation.
/// Uses long PK (bigserial) for high volume.
/// </summary>
public class PriceHistory
{
    public long Id { get; set; }

    /// <summary>
    /// FK -> StockListing.TickerSymbol
    /// </summary>
    public string TickerSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Close/current price at snapshot.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Day high at snapshot time.
    /// </summary>
    public decimal? High { get; set; }

    /// <summary>
    /// Day low at snapshot time.
    /// </summary>
    public decimal? Low { get; set; }

    /// <summary>
    /// Day open at snapshot time.
    /// </summary>
    public decimal? Open { get; set; }

    /// <summary>
    /// Previous close price.
    /// </summary>
    public decimal? PrevClose { get; set; }

    /// <summary>
    /// UTC snapshot timestamp.
    /// </summary>
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
