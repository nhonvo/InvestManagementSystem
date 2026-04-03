namespace InventoryAlert.Api.Infrastructure.Persistence.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        public Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken);
        public Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken);
        public Task<T> AddAsync(T entity, CancellationToken cancellationToken);
        public Task AddRangeAsync(IEnumerable<T> entity, CancellationToken cancellationToken);
        public Task<T> UpdateAsync(T entity);
        public Task UpdateRangeAsync(IEnumerable<T> entities);
        public Task<T> DeleteAsync(T entity);
    }
}
