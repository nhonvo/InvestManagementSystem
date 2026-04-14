namespace InventoryAlert.Domain.Entities.Postgres;

/// <summary>
/// Cached Basic Financials. One row per ticker. Updated daily by worker.
/// Maps to Finnhub /stock/metric.
/// </summary>
public class StockMetric
{
    /// <summary>
    /// PK + FK -> StockListing.TickerSymbol
    /// </summary>
    public string TickerSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Price-to-Earnings ratio (TTM).
    /// </summary>
    public double? PeRatio { get; set; }

    /// <summary>
    /// Price-to-Book ratio.
    /// </summary>
    public double? PbRatio { get; set; }

    /// <summary>
    /// Earnings per share (TTM).
    /// </summary>
    public double? EpsBasicTtm { get; set; }

    /// <summary>
    /// Dividend yield (annual %).
    /// </summary>
    public double? DividendYield { get; set; }

    /// <summary>
    /// 52-week high price.
    /// </summary>
    public decimal? Week52High { get; set; }

    /// <summary>
    /// 52-week low price.
    /// </summary>
    public decimal? Week52Low { get; set; }

    /// <summary>
    /// YoY revenue growth (TTM).
    /// </summary>
    public double? RevenueGrowthTtm { get; set; }

    /// <summary>
    /// Net profit margin (TTM).
    /// </summary>
    public double? MarginNet { get; set; }

    /// <summary>
    /// UTC timestamp of last Finnhub sync.
    /// </summary>
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;
}
