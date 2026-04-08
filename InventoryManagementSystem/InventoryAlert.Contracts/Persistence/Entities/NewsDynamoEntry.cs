using Amazon.DynamoDBv2.DataModel;

namespace InventoryAlert.Contracts.Persistence.Entities;

[DynamoDBTable("inventory-news")]
public class NewsDynamoEntry
{
    [DynamoDBHashKey]
    public string TickerSymbol { get; set; } = string.Empty;

    [DynamoDBRangeKey]
    public string PublishedAt { get; set; } = string.Empty;

    public string Headline { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public long FinnhubId { get; set; }

    // For automatic data expiration (optional, e.g. 30 days)
    public long Ttl { get; set; }
}
