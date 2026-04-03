using InventoryAlert.Api.Domain.Interfaces;

namespace InventoryAlert.Api.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Coordinates atomic transactions across one or more repositories.
    /// Use when an operation spans multiple DB writes that must succeed or fail together.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _dbContext;
        public IProductRepository ProductRepository { get; }

        public UnitOfWork(AppDbContext dbContext)
        {
            _dbContext = dbContext;
            ProductRepository = new ProductRepository(_dbContext);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task ExecuteTransactionAsync(Action action, CancellationToken cancellationToken)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                action();
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new InvalidOperationException("Transaction failed and was rolled back.", ex);
            }
        }

        public async Task ExecuteTransactionAsync(Func<Task> action, CancellationToken cancellationToken)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await action();
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new InvalidOperationException("Transaction failed and was rolled back.", ex);
            }
        }
    }
}
