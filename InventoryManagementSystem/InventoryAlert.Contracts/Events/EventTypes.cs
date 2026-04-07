namespace InventoryAlert.Contracts.Events;

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

    /// <summary>Fired when a significant company news headline is detected via Finnhub.</summary>
    public const string CompanyNewsAlert = "inventoryalert.news.headline.v1";

    // ── Registry (for dispatcher registration & validation) ────────────────
    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        MarketPriceAlert,
        StockLowAlert,
        CompanyNewsAlert,
    };

    /// <summary>True if the given eventType is registered and handled by this application.</summary>
    public static bool IsKnown(string eventType) => All.Contains(eventType);
}
