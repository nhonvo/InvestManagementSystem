using Amazon.DynamoDBv2;
using InventoryAlert.Contracts.Persistence.Entities;
using InventoryAlert.Contracts.Persistence.Interfaces;
using Microsoft.Extensions.Logging;

namespace InventoryAlert.Contracts.Persistence.Repositories;

public class EventLogDynamoRepository(IAmazonDynamoDB dynamoDbClient, ILogger<EventLogDynamoRepository> logger)
    : DynamoDbGenericRepository<EventLogEntry>(dynamoDbClient, logger), IEventLogDynamoRepository
{
}
