using InventoryAlert.Worker.Configuration;
using InventoryAlert.Worker.Interfaces;

namespace InventoryAlert.Worker.Infrastructure.MessageConsumers;

/// <summary>
/// Continuous polling logic for the Native SQS Background Worker.
/// Uses the unified ISqsDispatcher for reliable processing.
/// </summary>
public class ProcessQueueJob(
    ISqsHelper sqsHelper,
    ISqsDispatcher dispatcher,
    WorkerSettings settings,
    ILogger<ProcessQueueJob> logger) : IProcessQueueJob
{
    private readonly ISqsHelper _sqsHelper = sqsHelper;
    private readonly ISqsDispatcher _dispatcher = dispatcher;
    private readonly WorkerSettings _settings = settings;
    private readonly ILogger<ProcessQueueJob> _logger = logger;

    public async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("[NativeSQS] Continuous poll started on: {QueueUrl}", _settings.Aws.SqsQueueUrl);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var messages = await _sqsHelper.ReceiveMessagesAsync(_settings.Aws.SqsQueueUrl, ct: ct);
                
                if (messages.Count == 0) continue;

                _logger.LogInformation("[NativeSQS] Received {Count} messages.", messages.Count);
                await _dispatcher.ProcessBatchAsync(messages, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[NativeSQS] Polling loop error. Backing off for 5s.");
                await Task.Delay(5000, ct);
            }
        }
        
        _logger.LogInformation("[NativeSQS] Continuous poll stopped.");
    }
}
