using InventoryAlert.Contracts.Persistence;
using InventoryAlert.Contracts.Persistence.Interfaces;
using InventoryAlert.Contracts.Persistence.Repositories;
using Amazon.DynamoDBv2;
using InventoryAlert.Api.Domain.Interfaces;

namespace InventoryAlert.Api.Infrastructure.Persistence.Repositories;

public class EventLogRepository(IAmazonDynamoDB dynamoClient, ILogger<EventLogRepository> logger) 
    : DynamoDbGenericRepository<EventLog>(dynamoClient, logger), IEventLogRepository
{
    public async Task<EventLog> AddAsync(EventLog entry, CancellationToken ct)
    {
        await SaveAsync(entry, ct);
        return entry;
    }

    public Task<IEnumerable<EventLog>> GetAllAsync(CancellationToken ct)
    {
        // GetAllAsync is rarely used for logs in DynamoDB (usually filtered by EventType).
        // For simplicity during migration, returning empty or limited set.
        // The API already uses IEventLogQuery for list views.
        return Task.FromResult<IEnumerable<EventLog>>(Enumerable.Empty<EventLog>());
    }
}
