using InventoryAlert.Worker.Interfaces;

namespace InventoryAlert.Worker.Infrastructure.BackgroundServices;

/// <summary>
/// A native .NET BackgroundService that hosts the continuous SQS polling loop.
/// This matches the pattern of QueuedHostedService.cs.
/// </summary>
public class NativeSqsWorker(IProcessQueueJob processQueueJob, ILogger<NativeSqsWorker> logger) : BackgroundService
{
    private readonly IProcessQueueJob _processQueueJob = processQueueJob;
    private readonly ILogger<NativeSqsWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("[NativeSqsWorker] Starting native SQS polling loop...");

        try
        {
            // Run the continuous while loop inside the job
            await _processQueueJob.ExecuteAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in NativeSqsWorker.");
        }

        _logger.LogInformation("[NativeSqsWorker] Stopped native SQS polling loop.");
    }
}
