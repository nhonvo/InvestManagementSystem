namespace InventoryAlert.Api.Domain.Interfaces;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken);
    Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(int skip, int take, CancellationToken cancellationToken);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken);
    Task<T> UpdateAsync(T entity);
    Task UpdateRangeAsync(IEnumerable<T> entities);
    Task<T> DeleteAsync(T entity);
}
