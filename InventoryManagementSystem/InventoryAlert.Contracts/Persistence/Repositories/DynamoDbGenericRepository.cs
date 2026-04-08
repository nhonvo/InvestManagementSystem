using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using InventoryAlert.Contracts.Persistence.Interfaces;
using Microsoft.Extensions.Logging;

namespace InventoryAlert.Contracts.Persistence.Repositories;

public class DynamoDbGenericRepository<T>(IAmazonDynamoDB dynamoDbClient, ILogger<DynamoDbGenericRepository<T>> logger)
    : IDynamoDbGenericRepository<T> where T : class
{
    protected readonly IDynamoDBContext _context = new DynamoDBContextBuilder()
        .WithDynamoDBClient(() => dynamoDbClient)
        .Build();
    protected readonly ILogger<DynamoDbGenericRepository<T>> _logger = logger;

    public virtual async Task SaveAsync(T entity, CancellationToken ct = default)
    {
        try
        {
            await _context.SaveAsync(entity, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DynamoDB] Failed to save entity of type {Type}.", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<T?> GetByIdAsync(object hashKey, CancellationToken ct = default)
    {
        try
        {
            return await _context.LoadAsync<T>(hashKey, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DynamoDB] Failed to load entity of type {Type} with HashKey: {Key}.", typeof(T).Name, hashKey);
            return null;
        }
    }

    public virtual async Task<T?> GetByIdAsync(object hashKey, object rangeKey, CancellationToken ct = default)
    {
        try
        {
            return await _context.LoadAsync<T>(hashKey, rangeKey, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DynamoDB] Failed to load entity of type {Type} with RangeKey: {HKey}/{RKey}.", typeof(T).Name, hashKey, rangeKey);
            return null;
        }
    }

    public virtual async Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        try
        {
            await _context.DeleteAsync(entity, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DynamoDB] Failed to delete entity of type {Type}.", typeof(T).Name);
            throw;
        }
    }
}
