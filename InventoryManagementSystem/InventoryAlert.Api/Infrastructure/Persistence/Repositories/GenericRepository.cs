using InventoryAlert.Api.Infrastructure.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Api.Infrastructure.Persistence.Repositories
{
    public class GenericRepository<T>(AppDbContext dbContext) : IGenericRepository<T> where T : class
    {
        private readonly DbSet<T> _dbSet = dbContext.Set<T>();

        public async Task<T> AddAsync(T entity, CancellationToken cancellationToken)
        {
            var result = await _dbSet.AddAsync(entity, cancellationToken);
            return result.Entity;
        }
        public async Task AddRangeAsync(IEnumerable<T> entity, CancellationToken cancellationToken)
        {
            await _dbSet.AddRangeAsync(entity, cancellationToken);
        }

        public async Task<T> DeleteAsync(T entity)
        {
            var result = _dbSet.Remove(entity);
            return result.Entity;
        }

        public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken)
        {
            var result = await _dbSet.ToListAsync(cancellationToken);
            return result;
        }

        public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var result = await _dbSet.FindAsync(id, cancellationToken);
            return result;
        }

        public async Task<T> UpdateAsync(T entity)
        {
            var result = _dbSet.Update(entity);
            return result.Entity;
        }
        public async Task UpdateRangeAsync(IEnumerable<T> entities)
        {
            _dbSet.UpdateRange(entities);
        }
    }
}
