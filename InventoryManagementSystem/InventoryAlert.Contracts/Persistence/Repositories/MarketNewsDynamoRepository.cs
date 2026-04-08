using Amazon.DynamoDBv2;
using InventoryAlert.Contracts.Persistence.Entities;
using InventoryAlert.Contracts.Persistence.Interfaces;
using Microsoft.Extensions.Logging;

namespace InventoryAlert.Contracts.Persistence.Repositories;

public class MarketNewsDynamoRepository(IAmazonDynamoDB dynamoDbClient, ILogger<MarketNewsDynamoRepository> logger)
    : DynamoDbGenericRepository<MarketNewsDynamoEntry>(dynamoDbClient, logger), IMarketNewsDynamoRepository
{
}
