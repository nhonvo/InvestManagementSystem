namespace InventoryAlert.Contracts.Configuration;

public class SharedAwsSettings
{
    public string EndpointUrl { get; set; } = string.Empty;
    public string SnsTopicArn { get; set; } = string.Empty;
    public string SqsQueueUrl { get; set; } = string.Empty;
    public string SqsDlqUrl { get; set; } = string.Empty;
    public string DynamoDbTableName { get; set; } = "inventory-event-logs";
    public string DynamoDbNewsTableName { get; set; } = "inventory-news";
}
