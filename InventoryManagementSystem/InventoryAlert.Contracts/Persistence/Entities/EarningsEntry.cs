using Amazon.DynamoDBv2.DataModel;

namespace InventoryAlert.Contracts.Persistence.Entities;

[DynamoDBTable("inventory-earnings")]
public class EarningsEntry
{
    [DynamoDBHashKey]
    public string Symbol { get; set; } = string.Empty;

    [DynamoDBRangeKey]
    public string Period { get; set; } = string.Empty;

    public decimal Actual { get; set; }
    public decimal Estimate { get; set; }
    public decimal Surprise { get; set; }
    public decimal SurprisePercent { get; set; }
    
    public long Ttl { get; set; }
}
