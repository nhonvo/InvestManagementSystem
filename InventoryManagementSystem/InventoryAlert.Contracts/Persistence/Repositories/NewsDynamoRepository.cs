using Amazon.DynamoDBv2;
using InventoryAlert.Contracts.Persistence.Entities;
using InventoryAlert.Contracts.Persistence.Interfaces;
using Microsoft.Extensions.Logging;

namespace InventoryAlert.Contracts.Persistence.Repositories;

public class NewsDynamoRepository(IAmazonDynamoDB dynamoDbClient, ILogger<NewsDynamoRepository> logger)
    : DynamoDbGenericRepository<NewsDynamoEntry>(dynamoDbClient, logger), INewsDynamoRepository
{


    public async Task<IEnumerable<NewsDynamoEntry>> GetNewsByTickerAsync(string tickerSymbol, int limit = 10, CancellationToken ct = default)
    {
        try
        {
            // The IDynamoDBContext handles the Query translation automatically via the HashKey
            var search = _context.QueryAsync<NewsDynamoEntry>(tickerSymbol);
            var results = await search.GetNextSetAsync(ct);
            return results.Take(limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DynamoDB] Failed to query news for ticker {TickerSymbol}",
               tickerSymbol);
            return Enumerable.Empty<NewsDynamoEntry>();
        }
    }
}
