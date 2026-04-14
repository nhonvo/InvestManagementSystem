using InventoryAlert.Worker.Configuration;
using InventoryAlert.Worker.Interfaces;

namespace InventoryAlert.Worker.Hosting;
/// <summary>
/// A native .NET BackgroundService that hosts the continuous SQS polling loop.
/// This matches the pattern of QueuedHostedService.cs.
/// </summary>
public class SqsListenerService(
    IProcessQueueJob processQueueJob,
    WorkerSettings settings,
    ILogger<SqsListenerService> logger) : BackgroundService
{
    private readonly IProcessQueueJob _processQueueJob = processQueueJob;
    private readonly WorkerSettings _settings = settings;
    private readonly ILogger<SqsListenerService> _logger = logger;
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!_settings.IsPollMessage)
        {
            _logger.LogInformation("[NativeSqsWorker] Native polling is disabled in configuration. Service idling.");
            return;
        }
        _logger.LogInformation("[NativeSqsWorker] Starting native SQS polling loop...");
        try
        {
            // Run the continuous while loop inside the job
            await _processQueueJob.ExecuteAsync(ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[NativeSqsWorker] Cancellation requested. Shutting down.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in NativeSqsWorker.");
        }
        _logger.LogInformation("[NativeSqsWorker] Stopped native SQS polling loop.");
    }
}




