using Amazon.DynamoDBv2.DataModel;

namespace InventoryAlert.Contracts.Persistence.Entities;

[DynamoDBTable("inventory-recommendations")]
public class RecommendationEntry
{
    [DynamoDBHashKey]
    public string Symbol { get; set; } = string.Empty;

    [DynamoDBRangeKey]
    public string Period { get; set; } = string.Empty;

    public int StrongBuy { get; set; }
    public int Buy { get; set; }
    public int Hold { get; set; }
    public int Sell { get; set; }
    public int StrongSell { get; set; }

    public long Ttl { get; set; }
}
