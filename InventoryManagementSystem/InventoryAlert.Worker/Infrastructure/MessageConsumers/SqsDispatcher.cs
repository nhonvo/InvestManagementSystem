using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using InventoryAlert.Contracts.Configuration;
using InventoryAlert.Contracts.Events;
using InventoryAlert.Contracts.Events.Payloads;
using InventoryAlert.Contracts.Persistence.Entities;
using InventoryAlert.Contracts.Persistence.Interfaces;
using InventoryAlert.Worker.Configuration;
using InventoryAlert.Worker.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace InventoryAlert.Worker.Infrastructure.MessageConsumers;

public class SqsDispatcher(
    IAmazonSQS sqs,
    IMessageProcessor processor,
    IDistributedCache cache,
    IConnectionMultiplexer redis,
    WorkerSettings settings,
    ILogger<SqsDispatcher> logger) : ISqsDispatcher
{
    private readonly IAmazonSQS _sqs = sqs;
    private readonly IMessageProcessor _processor = processor;
    private readonly IDatabase _redisDb = redis.GetDatabase();
    private readonly IDistributedCache _cache = cache;
    private readonly WorkerSettings _settings = settings;
    private readonly ILogger<SqsDispatcher> _logger = logger;

    private const int MaxRetries = 3;

    public async Task ProcessBatchAsync(IEnumerable<Message> messages, CancellationToken ct)
    {
        foreach (var message in messages)
        {
            var success = await DispatchAsync(message, ct);
            if (success)
            {
                await _sqs.DeleteMessageAsync(_settings.Aws.SqsQueueUrl, message.ReceiptHandle, ct);
            }
        }
    }

    public async Task<bool> DispatchAsync(Message message, CancellationToken ct)
    {
        // 1. Retry Guard
        var receiveCount = GetReceiveCount(message);
        if (receiveCount > MaxRetries)
        {
            _logger.LogError("[Dispatcher] Message {Id} exceeded retries ({Count}). Moving manually to DLQ.", message.MessageId, receiveCount);
            await MoveToDlqAsync(message, ct);
            return true;
        }

        // 2. Deserialize Envelope
        var envelope = TryDeserializeEnvelope(message);
        if (envelope == null) return true; // ACK bad JSON

        // 3. Trace Context
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MessageId"] = envelope.MessageId,
            ["EventType"] = envelope.EventType,
            ["CorrelationId"] = envelope.CorrelationId
        });

        // 4. Atomic Deduplication (Redis)
        var dedupKey = $"msg:processed:{envelope.MessageId}";
        if (!await _redisDb.StringSetAsync(dedupKey, "1", TimeSpan.FromMinutes(30), When.NotExists))
        {
            _logger.LogInformation("[Dispatcher] Duplicate message detected. Skipping.");
            return true;
        }

        // 5. Business Logic Deduplication (e.g. Price Alert cooldown)
        if (envelope.EventType == EventTypes.MarketPriceAlert && await IsSupressedAsync(envelope, ct))
        {
            return true;
        }

        // 6. Execution
        try
        {
            _logger.LogInformation("[Dispatcher] Handing off to MessageProcessor.");
            await _processor.ProcessMessageAsync(message, ct);
            await _redisDb.KeyExpireAsync(dedupKey, TimeSpan.FromHours(48));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Dispatcher] Processing failed.");
            return false; // Redeliver
        }
    }

    private EventEnvelope? TryDeserializeEnvelope(Message message)
    {
        try
        {
            return JsonSerializer.Deserialize<EventEnvelope>(message.Body, JsonOptions.Default);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "[Dispatcher] Message {Id} body is not a valid EventEnvelope.", message.MessageId);
            return null;
        }
    }

    private async Task<bool> IsSupressedAsync(EventEnvelope envelope, CancellationToken ct)
    {
        // Price alert cooldown check
        try
        {
            var payload = JsonSerializer.Deserialize<MarketPriceAlertPayload>(envelope.Payload, JsonOptions.Default);
            if (payload == null) return false;

            var alertKey = $"alert:cooldown:{payload.Symbol}";
            if (await _cache.GetStringAsync(alertKey, ct) != null)
            {
                _logger.LogInformation("[Dispatcher] Alert for {Symbol} supressed by cooldown.", payload.Symbol);
                return true;
            }

            await _cache.SetStringAsync(alertKey, "1", new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) }, ct);
            return false;
        }
        catch { return false; }
    }

    private async Task MoveToDlqAsync(Message message, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_settings.Aws.SqsDlqUrl)) return;
        try
        {
            await _sqs.SendMessageAsync(new SendMessageRequest { QueueUrl = _settings.Aws.SqsDlqUrl, MessageBody = message.Body }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Dispatcher] Failed to move message {Id} to DLQ.", message.MessageId);
        }
    }

    private static int GetReceiveCount(Message message) =>
        message.Attributes != null && message.Attributes.TryGetValue("ApproximateReceiveCount", out var iStr) && int.TryParse(iStr, out var i) ? i : 1;
}
