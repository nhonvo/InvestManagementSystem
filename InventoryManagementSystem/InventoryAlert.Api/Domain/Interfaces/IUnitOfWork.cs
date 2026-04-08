namespace InventoryAlert.Api.Domain.Interfaces;

public interface IUnitOfWork
{
    IProductRepository Products { get; }
    IWatchlistRepository Watchlists { get; }
    IAlertRuleRepository AlertRules { get; }
    ICompanyProfileRepository CompanyProfiles { get; }
    IUserRepository Users { get; }
    
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task ExecuteTransactionAsync(Action action, CancellationToken cancellationToken);
    Task ExecuteTransactionAsync(Func<Task> action, CancellationToken cancellationToken);
}
