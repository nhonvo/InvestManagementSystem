using InventoryAlert.Api.Infrastructure.Persistence.Interfaces;

namespace InventoryAlert.Api.Infrastructure.Persistence.Repositories
{
    public class UnitOfWork: IUnitOfWork // TODO: review unit of work pattern, consider to use repository pattern only if the project is simple, otherwise, consider to use both unit of work and repository pattern together
    {
        private readonly AppDbContext _dbContext;
        public IProductRepository _productRepository { get; }
        
        public UnitOfWork(AppDbContext dbContext)
        {
            _dbContext = dbContext;
            _productRepository = new ProductRepository(_dbContext);
        }

        public async Task SaveChangesAsync(CancellationToken token)
        {
            await _dbContext.SaveChangesAsync(token);
        }

        public async Task ExecuteTransactionAsync(Action action, CancellationToken token)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync(token);
            try
            {
                action();
                await _dbContext.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                throw new Exception("Something wrong when excute",ex);
            }
        }

        public async Task ExecuteTransactionAsync(Func<Task> action, CancellationToken token)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync(token);
            try
            {
                await action();
                await _dbContext.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                throw new Exception("Something wrong when excute",ex);
            }
        }
    }
}
