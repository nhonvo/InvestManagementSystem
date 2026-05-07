using InventoryAlert.Domain.Interfaces;

namespace InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly AppDbContext _context;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        StockListings = new StockListingRepository(_context);
        WatchlistItems = new WatchlistItemRepository(_context);
        PriceHistories = new PriceHistoryRepository(_context);
        AlertRules = new AlertRuleRepository(_context);
        Users = new UserRepository(_context);
        Trades = new TradeRepository(_context);
        Metrics = new StockMetricRepository(_context);
        Earnings = new EarningsSurpriseRepository(_context);
        Recommendations = new RecommendationTrendRepository(_context);
        Insiders = new InsiderTransactionRepository(_context);
        Notifications = new NotificationRepository(_context);
    }

    public IStockListingRepository StockListings { get; }
    public IWatchlistItemRepository WatchlistItems { get; }
    public IPriceHistoryRepository PriceHistories { get; }
    public IAlertRuleRepository AlertRules { get; }
    public IUserRepository Users { get; }
    public ITradeRepository Trades { get; }
    public IStockMetricRepository Metrics { get; }
    public IEarningsSurpriseRepository Earnings { get; }
    public IRecommendationTrendRepository Recommendations { get; }
    public IInsiderTransactionRepository Insiders { get; }
    public INotificationRepository Notifications { get; }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ExecuteTransactionAsync(Action action, CancellationToken cancellationToken)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            action();
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ExecuteTransactionAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await action();
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<T> ExecuteSynchronizedAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return await action();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ExecuteSynchronizedAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await action();
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        _lock.Dispose();
        GC.SuppressFinalize(this);
    }
}
