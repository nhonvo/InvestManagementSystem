using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Web.Configuration;

namespace InventoryAlert.Api.Infrastructure.External;

public class DynamoDbEventLogQuery(IAmazonDynamoDB dynamoDb, AppSettings settings) : IEventLogQuery
{
    private readonly IAmazonDynamoDB _dynamoDb = dynamoDb;
    private readonly string _tableName = settings.Aws.DynamoDbTableName;

    public async Task<IEnumerable<EventLog>> GetRecentEventsAsync(string eventType, int limit = 20, CancellationToken ct = default)
    {
        var request = new QueryRequest
        {
            TableName = _tableName,
            KeyConditionExpression = "EventType = :v_eventType",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":v_eventType", new AttributeValue { S = eventType } }
            },
            ScanIndexForward = false, // Sort descending (latest first) requires SortKey. Here we just sort.
            Limit = limit
        };

        var response = await _dynamoDb.QueryAsync(request, ct);

        return response.Items.Select(item => new EventLog
        {
            MessageId = item.GetValueOrDefault("MessageId")?.S ?? string.Empty,
            EventType = item.GetValueOrDefault("EventType")?.S ?? string.Empty,
            Source = item.GetValueOrDefault("Source")?.S ?? string.Empty,
            Payload = item.GetValueOrDefault("Payload")?.S ?? string.Empty,
            Status = item.GetValueOrDefault("Status")?.S ?? string.Empty,
            ProcessedAt = item.GetValueOrDefault("ProcessedAt")?.S ?? string.Empty
        });
    }
}
