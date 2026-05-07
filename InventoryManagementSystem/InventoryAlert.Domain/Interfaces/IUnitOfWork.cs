namespace InventoryAlert.Domain.Interfaces;

public interface IUnitOfWork
{
    IStockListingRepository StockListings { get; }
    IWatchlistItemRepository WatchlistItems { get; }
    IPriceHistoryRepository PriceHistories { get; }
    IAlertRuleRepository AlertRules { get; }
    IUserRepository Users { get; }
    ITradeRepository Trades { get; }
    IStockMetricRepository Metrics { get; }
    IEarningsSurpriseRepository Earnings { get; }
    IRecommendationTrendRepository Recommendations { get; }
    IInsiderTransactionRepository Insiders { get; }
    INotificationRepository Notifications { get; }

    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task ExecuteTransactionAsync(Action action, CancellationToken cancellationToken);
    Task ExecuteTransactionAsync(Func<Task> action, CancellationToken cancellationToken);
    Task<T> ExecuteSynchronizedAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken);
    Task ExecuteSynchronizedAsync(Func<Task> action, CancellationToken cancellationToken);
}
