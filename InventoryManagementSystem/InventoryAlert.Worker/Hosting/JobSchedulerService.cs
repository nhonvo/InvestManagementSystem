using Hangfire;
using InventoryAlert.Worker.Configuration;
using InventoryAlert.Worker.IntegrationEvents.Handlers;
using InventoryAlert.Worker.ScheduledJobs;

namespace InventoryAlert.Worker.Hosting;

public sealed class JobSchedulerService(
    IRecurringJobManager recurringJobs,
    WorkerSettings settings,
    ILogger<JobSchedulerService> logger) : IHostedService
{
    private readonly IRecurringJobManager _recurringJobs = recurringJobs;
    private readonly WorkerSettings _settings = settings;
    private readonly ILogger _logger = logger;

    public Task StartAsync(CancellationToken ct)
    {
        var s = _settings.Schedules;

        _recurringJobs.AddOrUpdate<SyncPricesJob>(
            "sync-prices",
            x => x.ExecuteAsync(CancellationToken.None),
            s.SyncPrices);

        _recurringJobs.AddOrUpdate<SyncMetricsJob>(
            "sync-metrics",
            x => x.ExecuteAsync(CancellationToken.None),
            s.SyncMetrics);

        _recurringJobs.AddOrUpdate<SyncEarningsJob>(
            "sync-earnings",
            x => x.ExecuteAsync(CancellationToken.None),
            s.SyncEarnings);

        _recurringJobs.AddOrUpdate<SyncRecommendationsJob>(
            "sync-recommendations",
            x => x.ExecuteAsync(CancellationToken.None),
            s.SyncRecommendations);

        _recurringJobs.AddOrUpdate<SyncInsidersJob>(
            "sync-insiders",
            x => x.ExecuteAsync(CancellationToken.None),
            s.SyncInsiders);

        _recurringJobs.AddOrUpdate<SyncMarketNewsHandler>(
            "market-news",
            x => x.HandleAsync(CancellationToken.None),
            s.MarketNews);

        _recurringJobs.AddOrUpdate<CompanyNewsJob>(
            "company-news",
            x => x.ExecuteAsync(CancellationToken.None),
            s.NewsCheck);

        _recurringJobs.AddOrUpdate<CleanupPriceHistoryJob>(
            "cleanup-prices",
            x => x.ExecuteAsync(CancellationToken.None),
            s.CleanupPrices);

        _logger.LogInformation("[JobSchedulerService] All intelligence and cleanup jobs registered.");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
