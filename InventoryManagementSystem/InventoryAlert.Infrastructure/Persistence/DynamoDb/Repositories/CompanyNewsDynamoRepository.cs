using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using InventoryAlert.Domain.Entities.Dynamodb;
using InventoryAlert.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace InventoryAlert.Infrastructure.Persistence.DynamoDb.Repositories;

public class CompanyNewsDynamoRepository(IAmazonDynamoDB dynamoDbClient, ILogger<CompanyNewsDynamoRepository> logger)
    : DynamoDbGenericRepository<CompanyNewsDynamoEntry>(dynamoDbClient, logger), ICompanyNewsDynamoRepository
{
    public async Task<IEnumerable<CompanyNewsDynamoEntry>> GetLatestBySymbolAsync(string symbol, int limit, CancellationToken ct)
    {
        return await _context.FromQueryAsync<CompanyNewsDynamoEntry>(new QueryOperationConfig
        {
            Limit = limit,
            BackwardSearch = true,
            Filter = new QueryFilter("PK", QueryOperator.Equal, $"SYMBOL#{symbol}"),
        }).GetRemainingAsync(ct);
    }
}
