using Amazon.DynamoDBv2;
using InventoryAlert.Domain.Entities.Dynamodb;
using InventoryAlert.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace InventoryAlert.Infrastructure.Persistence.DynamoDb.Repositories;

public class MarketNewsDynamoRepository(IAmazonDynamoDB dynamoDbClient, ILogger<MarketNewsDynamoRepository> logger)
    : DynamoDbGenericRepository<MarketNewsDynamoEntry>(dynamoDbClient, logger), IMarketNewsDynamoRepository
{
}
