using Amazon.DynamoDBv2;
using InventoryAlert.Contracts.Persistence.Entities;
using InventoryAlert.Contracts.Persistence.Interfaces;
using Microsoft.Extensions.Logging;

namespace InventoryAlert.Contracts.Persistence.Repositories;

public class RecommendationRepository(IAmazonDynamoDB dynamoDbClient, ILogger<RecommendationRepository> logger)
    : DynamoDbGenericRepository<RecommendationEntry>(dynamoDbClient, logger), IRecommendationRepository
{
}
