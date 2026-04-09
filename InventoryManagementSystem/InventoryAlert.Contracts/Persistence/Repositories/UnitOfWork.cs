using InventoryAlert.Contracts.Common.Constants;
using InventoryAlert.Contracts.Common.Exceptions;
using InventoryAlert.Contracts.Persistence.Interfaces;

namespace InventoryAlert.Contracts.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly InventoryDbContext _dbContext;

    public IProductRepository Products { get; }
    public IWatchlistRepository Watchlists { get; }
    public IAlertRuleRepository AlertRules { get; }
    public ICompanyProfileRepository CompanyProfiles { get; }
    public IUserRepository Users { get; }
    public IStockTransactionRepository StockTransactions { get; }

    public UnitOfWork(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
        Products = new ProductRepository(_dbContext);
        Watchlists = new WatchlistRepository(_dbContext);
        AlertRules = new AlertRuleRepository(_dbContext);
        CompanyProfiles = new CompanyProfileRepository(_dbContext);
        Users = new UserRepository(_dbContext);
        StockTransactions = new StockTransactionRepository(_dbContext);
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
            throw new UserFriendlyException(ErrorCode.Internal, ApplicationConstants.Messages.TransactionFailed, ex);
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
            throw new UserFriendlyException(ErrorCode.Internal, ApplicationConstants.Messages.TransactionFailed, ex);
        }
    }
}
