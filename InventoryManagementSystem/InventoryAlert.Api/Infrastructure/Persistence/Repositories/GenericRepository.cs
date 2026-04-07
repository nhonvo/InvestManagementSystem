using InventoryAlert.Contracts.Persistence;
using InventoryAlert.Api.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Api.Infrastructure.Persistence.Repositories;

public class GenericRepository<T>(InventoryDbContext context) : IGenericRepository<T> where T : class
{
    private readonly DbSet<T> _dbSet = context.Set<T>();

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken)
    {
        var result = await _dbSet.AddAsync(entity, cancellationToken);
        return result.Entity;
    }

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    public Task<T> DeleteAsync(T entity)
    {
        var result = _dbSet.Remove(entity);
        return Task.FromResult(result.Entity);
    }

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(int skip, int take, CancellationToken cancellationToken)
    {
        skip = Math.Max(0, skip);
        take = Math.Max(1, take);

        var count = await _dbSet.CountAsync(cancellationToken);
        var items = await _dbSet
            .AsNoTracking()
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
        return (items, count);
    }

    public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _dbSet.FindAsync([id], cancellationToken);
    }

    public Task<T> UpdateAsync(T entity)
    {
        var result = _dbSet.Update(entity);
        return Task.FromResult(result.Entity);
    }

    public Task UpdateRangeAsync(IEnumerable<T> entities)
    {
        _dbSet.UpdateRange(entities);
        return Task.CompletedTask;
    }
}
