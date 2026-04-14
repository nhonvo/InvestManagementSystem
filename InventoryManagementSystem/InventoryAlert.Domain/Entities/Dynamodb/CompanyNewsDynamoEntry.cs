using Amazon.DynamoDBv2.DataModel;

namespace InventoryAlert.Domain.Entities.Dynamodb;

[DynamoDBTable("inventoryalert-company-news")]
public class CompanyNewsDynamoEntry
{
    /// <summary>
    /// Partition Key: SYMBOL#<ticker>
    /// </summary>
    [DynamoDBHashKey]
    public string PK { get; set; } = string.Empty;

    /// <summary>
    /// Sort Key: TS#<unix_timestamp_ms>
    /// </summary>
    [DynamoDBRangeKey]
    public string SK { get; set; } = string.Empty;

    public long NewsId { get; set; }
    public string Headline { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    public long Timestamp { get; set; }

    /// <summary>
    /// Denormalized ticker (for GSI).
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    public string SyncedAt { get; set; } = string.Empty;
}
