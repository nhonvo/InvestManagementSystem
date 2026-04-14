using InventoryAlert.Worker.Interfaces;

namespace InventoryAlert.Worker.Hosting;

public class BackgroundTaskProcessor(
    IBackgroundTaskQueue taskQueue,
    ILogger<BackgroundTaskProcessor> logger) : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue = taskQueue;
    private readonly ILogger<BackgroundTaskProcessor> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("[QueuedHostedService] Start background consumer.");
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var workItem = await _taskQueue.DequeueAsync(ct);
                if (workItem == null) continue;
                // Execute the work item
                await workItem(ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred executing background task.");
        }
        _logger.LogInformation("[QueuedHostedService] Stopped background consumer.");
    }
}



