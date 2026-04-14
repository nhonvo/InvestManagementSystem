namespace InventoryAlert.Domain.Constants;

/// <summary>
/// System-wide constants shared between Api, Worker, and any future service.
/// Centralised here to prevent magic strings drifting across projects.
/// </summary>
public static class AlertConstants
{
    // ─── Price Alert Defaults ────────────────────────────────────────────────
    /// <summary>Default threshold: alert when price drops more than 10%.</summary>
    public const double DefaultPriceAlertThreshold = 0.10;

    /// <summary>Minimum cooldown between repeated price alerts for the same symbol.</summary>
    public static readonly TimeSpan AlertCooldown = TimeSpan.FromHours(1);

    // ─── Stock Alert Defaults ────────────────────────────────────────────────
    /// <summary>Default threshold: alert when stock count drops below 5 units.</summary>
    public const int DefaultStockAlertThreshold = 5;
}



public static class CacheKeys
{
    public static string ProductQuote(string symbol) => $"product:quote:{symbol}";
    public static string AlertHistory(string symbol) => $"alert:history:{symbol}";
    public static string JobLastRun(string jobName) => $"job:last-run:{jobName}";
    public static string NewsLatest(string symbol) => $"news:{symbol}:latest";
}

public static class SqsHeaders
{
    public const string EventTypeAttribute = "EventType";
    public const string SourceServiceAttribute = "SourceService";
    public const string CorrelationIdAttribute = "CorrelationId";
}
