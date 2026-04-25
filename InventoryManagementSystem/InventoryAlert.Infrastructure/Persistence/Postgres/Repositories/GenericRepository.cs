using InventoryAlert.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;

public class GenericRepository<T>(AppDbContext context) : IGenericRepository<T> where T : class
{
    protected readonly AppDbContext _context = context;
    protected readonly DbSet<T> _dbSet = context.Set<T>();

    public virtual async Task<T?> GetByIdAsync(object id, CancellationToken ct)
    {
        return await _dbSet.FindAsync([id], cancellationToken: ct);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct)
    {
        return await _dbSet.AsNoTracking().ToListAsync(ct);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken ct)
    {
        await _dbSet.AddAsync(entity, ct);
        return entity;
    }

    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct)
    {
        await _dbSet.AddRangeAsync(entities, ct);
    }

    public virtual Task UpdateAsync(T entity, CancellationToken ct)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(T entity, CancellationToken ct)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }
}
