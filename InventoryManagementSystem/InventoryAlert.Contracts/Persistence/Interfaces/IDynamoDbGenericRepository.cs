using Amazon.DynamoDBv2.DataModel;

namespace InventoryAlert.Contracts.Persistence.Interfaces;

public interface IDynamoDbGenericRepository<T> where T : class
{
    Task SaveAsync(T entity, CancellationToken ct = default);
    Task<T?> GetByIdAsync(object hashKey, CancellationToken ct = default);
    Task<T?> GetByIdAsync(object hashKey, object rangeKey, CancellationToken ct = default);
    Task DeleteAsync(T entity, CancellationToken ct = default);
}
