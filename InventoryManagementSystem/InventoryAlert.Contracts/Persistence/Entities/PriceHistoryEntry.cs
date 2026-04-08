using Amazon.DynamoDBv2.DataModel;

namespace InventoryAlert.Contracts.Persistence.Entities;

[DynamoDBTable("inventory-price-history")]
public class PriceHistoryEntry
{
    [DynamoDBHashKey]
    public string TickerSymbol { get; set; } = string.Empty;

    [DynamoDBRangeKey]
    public string Timestamp { get; set; } = string.Empty;

    public decimal Price { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Open { get; set; }
    public decimal PrevClose { get; set; }

    public long Ttl { get; set; }
}
