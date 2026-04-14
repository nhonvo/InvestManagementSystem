namespace InventoryAlert.Domain.Entities.Postgres;

/// <summary>
/// Global Catalog. Read-only market reference data shared across the system. 
/// Seeded from Finnhub /stock/symbol and enriched via /stock/profile2.
/// </summary>
public class StockListing
{
    public int Id { get; set; }

    /// <summary>
    /// Company display name (e.g., "Tesla Inc").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique market ticker (e.g., "TSLA").
    /// </summary>
    public string TickerSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Exchange code (e.g., "NASDAQ").
    /// </summary>
    public string? Exchange { get; set; }

    /// <summary>
    /// ISO currency code (e.g., "USD").
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Domicile country code (e.g., "US").
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Finnhub industry classification.
    /// </summary>
    public string? Industry { get; set; }

    /// <summary>
    /// Market capitalization in USD.
    /// </summary>
    public decimal? MarketCap { get; set; }

    /// <summary>
    /// IPO date.
    /// </summary>
    public DateOnly? Ipo { get; set; }

    /// <summary>
    /// Company website URI.
    /// </summary>
    public string? WebUrl { get; set; }

    /// <summary>
    /// Company logo URI (from Finnhub profile2).
    /// </summary>
    public string? Logo { get; set; }

    /// <summary>
    /// UTC timestamp of last profile2 sync.
    /// </summary>
    public DateTime? LastProfileSync { get; set; }
}
