using Amazon.DynamoDBv2;
using InventoryAlert.Contracts.Persistence.Entities;
using InventoryAlert.Contracts.Persistence.Interfaces;
using Microsoft.Extensions.Logging;

namespace InventoryAlert.Contracts.Persistence.Repositories;

public class EarningsDynamoRepository(IAmazonDynamoDB dynamoDbClient, ILogger<EarningsDynamoRepository> logger)
    : DynamoDbGenericRepository<EarningsEntry>(dynamoDbClient, logger), IEarningsDynamoRepository
{
}
