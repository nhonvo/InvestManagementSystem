using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using InventoryAlert.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace InventoryAlert.Infrastructure.Persistence.DynamoDb.Repositories;

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

    public virtual async Task BatchSaveAsync(IEnumerable<T> entities, CancellationToken ct = default)
    {
        // DynamoDB batch writes are limited to 25 items per call.
        const int maxBatchSize = 25;
        var list = entities.ToList();

        for (int i = 0; i < list.Count; i += maxBatchSize)
        {
            var chunk = list.Skip(i).Take(maxBatchSize).ToList();
            try
            {
                var batch = _context.CreateBatchWrite<T>();
                batch.AddPutItems(chunk);
                await batch.ExecuteAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DynamoDB] Batch save failed for {Type} (chunk starting at {Index}).", typeof(T).Name, i);
                throw;
            }
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

    public virtual async Task<List<T>> QueryAsync(object hashKey, CancellationToken ct = default)
    {
        try
        {
            return await _context.QueryAsync<T>(hashKey, (DynamoDBOperationConfig)null!).GetRemainingAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DynamoDB] Failed to query entities of type {Type} with HashKey: {Key}.", typeof(T).Name, hashKey);
            return [];
        }
    }
}
