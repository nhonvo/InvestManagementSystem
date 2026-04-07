using InventoryAlert.Worker.Interfaces;

namespace InventoryAlert.Worker.Infrastructure.BackgroundServices;

public class QueuedHostedService(
    IBackgroundTaskQueue taskQueue,
    ILogger<QueuedHostedService> logger) : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue = taskQueue;
    private readonly ILogger<QueuedHostedService> _logger = logger;

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
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred executing background task.");
        }

        _logger.LogInformation("[QueuedHostedService] Stopped background consumer.");
    }
}
