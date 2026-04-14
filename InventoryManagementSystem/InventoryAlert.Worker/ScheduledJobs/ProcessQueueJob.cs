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
/// Now consolidated with dispatcher logic for reliable processing and reduced abstraction.
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
        // 1. Retry/DLQ Check
        if (message.Attributes != null &&
            message.Attributes.TryGetValue("ApproximateReceiveCount", out var receiveCountStr) &&
            int.TryParse(receiveCountStr, out var receiveCount) && receiveCount > 5)
        {
            _logger.LogWarning("[SqsWorker] Message {Id} exceeded retry limit ({Count}).", message.MessageId, receiveCount);
            // ACK to remove from main queue if it should be handled via Redrive Policy or DLQ
            return true;
        }

        // 2. Deserialize Envelope
        var envelope = TryDeserialize(message);
        if (envelope == null) return true; // ACK bad JSON

        // 3. Trace Context
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MessageId"] = envelope.MessageId,
            ["EventType"] = envelope.EventType,
            ["CorrelationId"] = envelope.CorrelationId
        });
        using var logContext = Serilog.Context.LogContext.PushProperty("CorrelationId", envelope.CorrelationId);

        // 4. Atomic Deduplication (Redis)
        var dedupKey = $"msg:processed:{envelope.MessageId}";
        if (!await _redisDb.StringSetAsync(dedupKey, "1", TimeSpan.FromMinutes(30), When.NotExists))
        {
            _logger.LogInformation("[SqsWorker] Duplicate message detected. Skipping.");
            return true;
        }

        // 5. Business Logic Deduplication (Price Alert cooldown)
        if (envelope.EventType == EventTypes.MarketPriceAlert && await IsSupressedAsync(envelope))
        {
            return true;
        }

        // 6. Execution
        try
        {
            await _router.ProcessMessageAsync(message, ct);
            await _redisDb.KeyExpireAsync(dedupKey, TimeSpan.FromHours(48));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SqsWorker] Processing failed.");
            return false; // Redeliver
        }
    }

    private EventEnvelope? TryDeserialize(Message message)
    {
        try
        {
            return JsonSerializer.Deserialize<EventEnvelope>(message.Body, JsonOptions.Default);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "[SqsWorker] Message {Id} body is not a valid EventEnvelope.", message.MessageId);
            return null;
        }
    }

    private async Task<bool> IsSupressedAsync(EventEnvelope envelope)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<MarketPriceAlertPayload>(envelope.Payload, JsonOptions.Default);
            if (payload == null) return false;

            var alertKey = $"alert:cooldown:{payload.Symbol}";
            if (await _redisDb.KeyExistsAsync(alertKey))
            {
                _logger.LogInformation("[SqsWorker] Alert for {Symbol} suppressed by cooldown.", payload.Symbol);
                return true;
            }

            await _redisDb.StringSetAsync(alertKey, "1", TimeSpan.FromHours(24));
            return false;
        }
        catch { return false; }
    }
}
