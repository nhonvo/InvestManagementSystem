using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
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
        var config = new DynamoDBOperationConfig
        {
            BackwardQuery = true
        };

#pragma warning disable CS0618
        var query = _context.QueryAsync<CompanyNewsDynamoEntry>($"SYMBOL#{symbol}", config);
#pragma warning restore CS0618
        var result = await query.GetNextSetAsync(ct);
        return result.Take(limit);
    }
}
