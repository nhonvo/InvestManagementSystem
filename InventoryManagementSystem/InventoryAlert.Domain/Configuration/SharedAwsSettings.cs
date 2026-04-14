namespace InventoryAlert.Domain.Configuration;

public class SharedAwsSettings
{
    public string EndpointUrl { get; set; } = string.Empty;
    public string SnsTopicArn { get; set; } = string.Empty;
    public string SqsQueueUrl { get; set; } = string.Empty;
}
