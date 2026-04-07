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

        // Configured minutes — price sync
        var syncMinutes = _settings.Finnhub.SyncIntervalMinutes > 0 ? _settings.Finnhub.SyncIntervalMinutes : 10;
        _recurringJobs.AddOrUpdate<SyncPricesJob>(
            "sync-prices",
            job => job.ExecuteAsync(CancellationToken.None),
            $"*/{syncMinutes} * * * *");

        // Every 1 hour — news check
        _recurringJobs.AddOrUpdate<NewsCheckJob>(
            "news-check",
            job => job.ExecuteAsync(CancellationToken.None),
            "0 * * * *");

        _logger.LogInformation("[JobSchedulerService] All recurring Hangfire jobs registered.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
