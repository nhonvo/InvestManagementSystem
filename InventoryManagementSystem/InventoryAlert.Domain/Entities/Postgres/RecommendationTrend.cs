namespace InventoryAlert.Domain.Entities.Postgres;

/// <summary>
/// Analyst Consensus. Monthly analyst buy/hold/sell counts.
/// Maps to Finnhub /stock/recommendation.
/// </summary>
public class RecommendationTrend
{
    public int Id { get; set; }

    /// <summary>
    /// FK -> StockListing.TickerSymbol
    /// </summary>
    public string TickerSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Month period (e.g., "2025-03-01").
    /// </summary>
    public string Period { get; set; } = string.Empty;

    public int StrongBuy { get; set; }
    public int Buy { get; set; }
    public int Hold { get; set; }
    public int Sell { get; set; }
    public int StrongSell { get; set; }

    /// <summary>
    /// UTC timestamp of last sync.
    /// </summary>
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
}
