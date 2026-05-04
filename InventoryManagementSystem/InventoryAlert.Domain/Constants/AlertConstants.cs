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
    // API Caches
    public static string Quote(string symbol) => $"inventoryalert:api:quote30s:v1:{symbol.ToUpperInvariant()}";
    public static string Metrics(string symbol) => $"inventoryalert:api:metrics1h:v1:{symbol.ToUpperInvariant()}";
    public static string Peers(string symbol) => $"inventoryalert:api:peers1d:v1:{symbol.ToUpperInvariant()}";
    
    public static string Search(string query)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(query.Trim().ToLowerInvariant()));
        var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        return $"inventoryalert:api:search4h:v1:{hash}";
    }

    // Worker/Infrastructure
    public static string AlertCooldown(Guid userId, Guid ruleId) => $"inventoryalert:alerts:cooldown:v1:{userId}:{ruleId}";
    public static string GlobalAlertCooldown(string symbol) => $"inventoryalert:worker:global-cooldown:v1:{symbol.ToUpperInvariant()}";
    public static string MessageProcessed(string messageId) => $"inventoryalert:worker:msg-processed:v1:{messageId}";

    // Legacy or internal 
    public static string JobLastRun(string jobName) => $"inventoryalert:worker:job-last-run:v1:{jobName}";
}

public static class SqsHeaders
{
    public const string EventTypeAttribute = "EventType";
    public const string SourceServiceAttribute = "SourceService";
    public const string CorrelationIdAttribute = "CorrelationId";
}
