namespace InventoryAlert.Contracts.Persistence.Interfaces;

public interface IUnitOfWork
{
    IProductRepository Products { get; }
    IWatchlistRepository Watchlists { get; }
    IAlertRuleRepository AlertRules { get; }
    ICompanyProfileRepository CompanyProfiles { get; }
    IUserRepository Users { get; }
    IStockTransactionRepository StockTransactions { get; }

    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task ExecuteTransactionAsync(Action action, CancellationToken cancellationToken);
    Task ExecuteTransactionAsync(Func<Task> action, CancellationToken cancellationToken);
}
