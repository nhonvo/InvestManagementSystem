namespace InventoryAlert.Domain.Entities.Postgres;

/// <summary>
/// Insider Activity. Last 100 insider transactions.
/// Maps to Finnhub /stock/insider-transactions.
/// </summary>
public class InsiderTransaction
{
    public int Id { get; set; }

    /// <summary>
    /// FK -> StockListing.TickerSymbol
    /// </summary>
    public string TickerSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Insider full name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Net share count (positive = buy, negative = sell).
    /// </summary>
    public long? Share { get; set; }

    /// <summary>
    /// Total transaction value in USD.
    /// </summary>
    public decimal? Value { get; set; }

    /// <summary>
    /// Date of transaction.
    /// </summary>
    public DateOnly? TransactionDate { get; set; }

    /// <summary>
    /// SEC filing date.
    /// </summary>
    public DateOnly? FilingDate { get; set; }

    /// <summary>
    /// SEC code (e.g., "P" = purchase, "S" = sale).
    /// </summary>
    public string? TransactionCode { get; set; }
}
