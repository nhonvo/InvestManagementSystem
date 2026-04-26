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
        // 1. Deserialize Envelope
        var envelope = TryDeserialize(message);
        if (envelope == null) return true; // ACK malformed JSON

        // 2. Logging & Tracing Context
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MessageId"] = envelope.MessageId,
            ["EventType"] = envelope.EventType,
            ["CorrelationId"] = envelope.CorrelationId
        });
        using var logContext = Serilog.Context.LogContext.PushProperty("CorrelationId", envelope.CorrelationId);

        // 3. Technical Deduplication Check (Idempotency)
        // Note: We check but DON'T set here to allow retries on failures.
        var dedupKey = $"msg:processed:{envelope.MessageId}";
        if (await _redisDb.KeyExistsAsync(dedupKey))
        {
            _logger.LogInformation("[SqsWorker] Duplicate message {MessageId} detected. Skipping.", envelope.MessageId);
            return true;
        }

        // 4. Execution
        try
        {
            var success = await _router.RouteEnvelopeAsync(envelope, ct);
            
            if (success)
            {
                // Mark as processed only on success
                await _redisDb.StringSetAsync(dedupKey, "1", TimeSpan.FromHours(24));
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SqsWorker] Processing failed for message {MessageId}.", envelope.MessageId);
            return false; // Leave in queue for SQS retry/DLQ
        }
    }

    private EventEnvelope? TryDeserialize(Message message)
    {
        try
        {
            return JsonSerializer.Deserialize<EventEnvelope>(message.Body, JsonOptions.Default);
        }
        catch (JsonException)
        {
            _logger.LogError("[SqsWorker] Message {Id} body is not a valid EventEnvelope.", message.MessageId);
            return null;
        }
    }
}
