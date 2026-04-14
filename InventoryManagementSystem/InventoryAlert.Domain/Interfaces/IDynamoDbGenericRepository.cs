namespace InventoryAlert.Domain.Interfaces;

public interface IDynamoDbGenericRepository<T> where T : class
{
    Task SaveAsync(T entity, CancellationToken ct = default);
    /// <summary>Batch-write up to 25 items per DynamoDB call (AWS limit).</summary>
    Task BatchSaveAsync(IEnumerable<T> entities, CancellationToken ct = default);
    Task<T?> GetByIdAsync(object hashKey, CancellationToken ct = default);
    Task<T?> GetByIdAsync(object hashKey, object rangeKey, CancellationToken ct = default);
    Task DeleteAsync(T entity, CancellationToken ct = default);
    Task<List<T>> QueryAsync(object hashKey, CancellationToken ct = default);
}
