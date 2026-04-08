using Hangfire;
using InventoryAlert.Worker.Configuration;
using InventoryAlert.Worker.Infrastructure.Jobs;
using InventoryAlert.Worker.Infrastructure.MessageConsumers;

namespace InventoryAlert.Worker.Infrastructure.BackgroundServices;

/// <summary>
/// One-shot hosted service that registers recurring Hangfire jobs on startup.
/// Uses IServiceScopeFactory to safely resolve scoped services.
/// </summary>
public sealed class JobSchedulerService(
    IRecurringJobManager recurringJobs,
    WorkerSettings settings,
    ILogger<JobSchedulerService> logger) : IHostedService
{
    private readonly IRecurringJobManager _recurringJobs = recurringJobs;
    private readonly WorkerSettings _settings = settings;
    private readonly ILogger<JobSchedulerService> _logger = logger;

    public Task StartAsync(CancellationToken ct)
    {
        // Use Native Polling or Hangfire Polling based on configuration
        if (!_settings.SqsPolling.UseNativeWorker)
        {
            _recurringJobs.AddOrUpdate<PollSqsJob>(
                "poll-sqs",
                job => job.ExecuteAsync(CancellationToken.None),
                "*/30 * * * * *");
        }
        else
        {
            _recurringJobs.RemoveIfExists("poll-sqs");
            _logger.LogInformation("[JobSchedulerService] Native polling is enabled. Hangfire 'poll-sqs' job disabled/removed.");
        }

        // Scheduled Background Jobs
        _recurringJobs.AddOrUpdate<SyncPricesJob>("sync-prices", x => x.ExecuteAsync(CancellationToken.None), "*/1 * * * *");
        _recurringJobs.AddOrUpdate<NewsCheckJob>("news-check", x => x.ExecuteAsync(CancellationToken.None), "*/5 * * * *");
        _recurringJobs.AddOrUpdate<MarketNewsJob>("market-news", x => x.ExecuteAsync(CancellationToken.None), "*/15 * * * *");
        _recurringJobs.AddOrUpdate<MarketStatusJob>("market-status", x => x.ExecuteAsync(CancellationToken.None), "*/5 * * * *");
        _recurringJobs.AddOrUpdate<RecommendationsJob>("recommendations", x => x.ExecuteAsync(CancellationToken.None), "0 6 * * *");
        _recurringJobs.AddOrUpdate<EarningsJob>("earnings", x => x.ExecuteAsync(CancellationToken.None), "0 7 * * *");
        _recurringJobs.AddOrUpdate<EarningsCalendarJob>("earnings-cal", x => x.ExecuteAsync(CancellationToken.None), "0 8 * * *");
        _recurringJobs.AddOrUpdate<ProfileSyncJob>("profile-sync", x => x.ExecuteAsync(CancellationToken.None), "0 2 * * 0");
        _recurringJobs.AddOrUpdate<SymbolCrawlJob>("symbol-crawl", x => x.ExecuteAsync(CancellationToken.None), "0 2 * * *");

        _logger.LogInformation("[JobSchedulerService] All recurring Hangfire jobs registered.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
