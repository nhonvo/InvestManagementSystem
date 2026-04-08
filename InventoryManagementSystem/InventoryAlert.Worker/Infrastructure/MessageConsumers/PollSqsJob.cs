using InventoryAlert.Worker.Configuration;
using InventoryAlert.Worker.Interfaces;

namespace InventoryAlert.Worker.Infrastructure.MessageConsumers;

/// <summary>
/// Hangfire recurring job: polls SQS once per execution and dispatches messages.
/// Uses the unified ISqsDispatcher for reliable processing.
/// </summary>
public class PollSqsJob(
    ISqsHelper sqsHelper,
    ISqsDispatcher dispatcher,
    WorkerSettings settings,
    ILogger<PollSqsJob> logger)
{
    private readonly ISqsHelper _sqsHelper = sqsHelper;
    private readonly ISqsDispatcher _dispatcher = dispatcher;
    private readonly WorkerSettings _settings = settings;
    private readonly ILogger<PollSqsJob> _logger = logger;

    /// <summary>
    /// Entry point for Hangfire.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[HangfireSQS] Starting poll execution.");

        try
        {
            // Fetch one batch and process it
            var messages = await _sqsHelper.ReceiveMessagesAsync(_settings.Aws.SqsQueueUrl, ct: ct);

            if (messages == null || messages.Count == 0)
            {
                _logger.LogInformation("[HangfireSQS] No messages found.");
                return;
            }

            await _dispatcher.ProcessBatchAsync(messages, ct);

            _logger.LogInformation("[HangfireSQS] Finished processing {Count} messages.", messages.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HangfireSQS] Job execution failed.");
            throw; // Rethrow so Hangfire sees it as a failure
        }
    }
}
