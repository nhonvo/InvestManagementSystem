using Amazon.DynamoDBv2;
using InventoryAlert.Contracts.Persistence.Entities;
using InventoryAlert.Contracts.Persistence.Interfaces;
using Microsoft.Extensions.Logging;

namespace InventoryAlert.Contracts.Persistence.Repositories;

public class PriceHistoryDynamoRepository(IAmazonDynamoDB dynamoDbClient, ILogger<PriceHistoryDynamoRepository> logger)
    : DynamoDbGenericRepository<PriceHistoryEntry>(dynamoDbClient, logger), IPriceHistoryDynamoRepository
{
}
