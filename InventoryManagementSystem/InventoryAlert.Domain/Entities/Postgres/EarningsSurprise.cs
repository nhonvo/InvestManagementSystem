namespace InventoryAlert.Domain.Entities.Postgres;

/// <summary>
/// Historical Earnings. Last 4 quarters of actual vs. estimated EPS.
/// Maps to Finnhub /stock/earnings.
/// </summary>
public class EarningsSurprise
{
    public int Id { get; set; }

    /// <summary>
    /// FK -> StockListing.TickerSymbol
    /// </summary>
    public string TickerSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Fiscal quarter end date (e.g., 2024-09-30).
    /// Unique constraint with TickerSymbol.
    /// </summary>
    public DateOnly Period { get; set; }

    /// <summary>
    /// Actual reported EPS.
    /// </summary>
    public double? ActualEps { get; set; }

    /// <summary>
    /// Consensus estimate EPS.
    /// </summary>
    public double? EstimateEps { get; set; }

    /// <summary>
    /// (Actual - Estimate) / |Estimate| * 100.
    /// </summary>
    public double? SurprisePercent { get; set; }

    /// <summary>
    /// Date of public earnings release.
    /// </summary>
    public DateOnly? ReportDate { get; set; }
}
