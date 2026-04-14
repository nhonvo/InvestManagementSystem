namespace InventoryAlert.Domain.Events;

/// <summary>
/// Canonical EventType strings. Format: "inventoryalert.{domain}.{action}.v{version}"
/// — reverse-DNS style keeps them globally unique and version-safe.
/// Used as the SNS Subject field and the SQS MessageAttribute EventType.
/// </summary>
public static class EventTypes
{
    // ── Pricing ────────────────────────────────────────────────────────────
    /// <summary>Fired when a product's current price drops below its configured threshold.</summary>
    public const string MarketPriceAlert = "inventoryalert.pricing.price-drop.v1";

    /// <summary>Fired when a product's stock count falls below StockAlertThreshold.</summary>
    public const string StockLowAlert = "inventoryalert.inventory.stock-low.v1";

    /// <summary>Fired when a news headline is detected via Finnhub.</summary>
    public const string CompanyNewsAlert = "inventoryalert.news.headline.v1";

    /// <summary>Fired by the UI to request a manual sync of market news.</summary>
    public const string SyncMarketNewsRequested = "inventoryalert.news.sync-requested.v1";

    /// <summary>Fired by the UI to request a manual sync of news for a specific ticker.</summary>
    public const string SyncCompanyNewsRequested = "inventoryalert.news.company-sync-requested.v1";

    public const string MarketNews = "inventoryalert.news.market-news.v1";

    // ── Registry (for dispatcher registration & validation) ────────────────
    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        MarketPriceAlert,
        StockLowAlert,
        CompanyNewsAlert,
        SyncMarketNewsRequested,
        SyncCompanyNewsRequested,
        MarketNews
    };

    /// <summary>True if the given eventType is registered and handled by this application.</summary>
    public static bool IsKnown(string eventType) => All.Contains(eventType);
}
