using InventoryAlert.Domain.Entities.Postgres;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Infrastructure.Persistence.Postgres;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<StockListing> StockListings => Set<StockListing>();
    public DbSet<WatchlistItem> WatchlistItems => Set<WatchlistItem>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<PriceHistory> PriceHistories => Set<PriceHistory>();
    public DbSet<StockMetric> StockMetrics => Set<StockMetric>();
    public DbSet<EarningsSurprise> EarningsSurprises => Set<EarningsSurprise>();
    public DbSet<RecommendationTrend> RecommendationTrends => Set<RecommendationTrend>();
    public DbSet<InsiderTransaction> InsiderTransactions => Set<InsiderTransaction>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
