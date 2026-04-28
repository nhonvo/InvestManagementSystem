using System.Text.Json;
using Amazon.SQS.Model;
using InventoryAlert.Domain.Configuration;
using InventoryAlert.Domain.Events;
using InventoryAlert.Domain.Events.Payloads;
using InventoryAlert.Worker.Configuration;
using InventoryAlert.Worker.Interfaces;
using StackExchange.Redis;

namespace InventoryAlert.Worker.ScheduledJobs;

/// <summary>
/// Continuous polling logic for the Native SQS Background Worker.
/// Coordinates deduplication, logging, and routing of EventEnvelopes.
/// </summary>
public class ProcessQueueJob(
    ISqsHelper sqsHelper,
    IIntegrationMessageRouter router,
    IConnectionMultiplexer redis,
    WorkerSettings settings,
    ILogger<ProcessQueueJob> logger) : IProcessQueueJob
{
    private readonly ISqsHelper _sqsHelper = sqsHelper;
    private readonly IIntegrationMessageRouter _router = router;
    private readonly IDatabase _redisDb = redis.GetDatabase();
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
                await ProcessBatchAsync(messages, ct);
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

    public async Task ProcessBatchAsync(IEnumerable<Message> messages, CancellationToken ct)
    {
        foreach (var message in messages)
        {
            var success = await DispatchInternalAsync(message, ct);
            if (success)
            {
                await _sqsHelper.DeleteMessageAsync(_settings.Aws.SqsQueueUrl, message.ReceiptHandle, ct);
            }
        }
    }

    private async Task<bool> DispatchInternalAsync(Message message, CancellationToken ct)
    {
        // 1. Technical Deduplication Check (Idempotency)
        // We check if we already finished this message id.
        var dedupKey = $"msg:processed:{message.MessageId}";
        if (await _redisDb.KeyExistsAsync(dedupKey))
        {
            _logger.LogInformation("[SqsWorker] Duplicate message {MessageId} detected. Skipping.", message.MessageId);
            return true;
        }

        // 2. Execution
        try
        {
            // Note: IntegrationMessageRouter now handles SNS unwrapping and deserialization
            var success = await _router.ProcessAndAcknowledgeAsync(message, ct);
            
            if (success)
            {
                // Mark as processed only on success
                await _redisDb.StringSetAsync(dedupKey, "1", TimeSpan.FromHours(24));
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SqsWorker] Processing failed for message {MessageId}.", message.MessageId);
            return false; // Leave in queue for SQS retry/DLQ
        }
    }
}
