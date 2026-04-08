using Amazon.DynamoDBv2.DataModel;

namespace InventoryAlert.Contracts.Persistence.Entities;

[DynamoDBTable("inventory-market-news")]
public class MarketNewsDynamoEntry
{
    [DynamoDBHashKey]
    public string Category { get; set; } = string.Empty;

    [DynamoDBRangeKey]
    public string PublishedAt { get; set; } = string.Empty;

    public string Headline { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public long FinnhubId { get; set; }

    public long Ttl { get; set; }
}
