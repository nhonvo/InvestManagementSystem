using System.Text.Json;
using Amazon.SQS.Model;
using Hangfire;
using InventoryAlert.Contracts.Configuration;
using InventoryAlert.Contracts.Events;
using InventoryAlert.Contracts.Events.Payloads;
using InventoryAlert.Worker.Application.Interfaces.Handlers;
using InventoryAlert.Worker.Interfaces;

namespace InventoryAlert.Worker.Application;

/// <summary>
/// Orchestrates message routing: all event types are dispatched to Hangfire for
/// persistent/retriable execution. Unknown messages are enqueued on the default Hangfire queue.
/// </summary>
public class MessageProcessor(
    IRawDefaultHandler rawHandler,
    IBackgroundJobClient backgroundJobs,
    ILogger<MessageProcessor> logger) : IMessageProcessor
{
    private readonly IRawDefaultHandler _rawHandler = rawHandler;
    private readonly IBackgroundJobClient _backgroundJobs = backgroundJobs;
    private readonly ILogger<MessageProcessor> _logger = logger;

    /// <summary>
    /// Processes a message and returns true if it should be deleted from SQS.
    /// </summary>
    public async Task<bool> ProcessAndAcknowledgeAsync(Message message, CancellationToken ct)
    {
        if (message.MessageAttributes == null || !message.MessageAttributes.TryGetValue("MessageType", out var attr))
        {
            _logger.LogWarning("[MessageProcessor] Message {MessageId} missing 'MessageType' attribute or attributes collection is null. Skipping.", message.MessageId);
            return true; // ACK to remove garbage from queue
        }

        var messageType = attr.StringValue;
        var correlationId = message.MessageAttributes.GetValueOrDefault("CorrelationId")?.StringValue ?? "N/A";

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MessageId"] = message.MessageId,
            ["MessageType"] = messageType,
            ["CorrelationId"] = correlationId
        });

        try
        {
            switch (messageType)
            {
                case string msgType when msgType.Equals(EventTypes.MarketPriceAlert, StringComparison.OrdinalIgnoreCase):
                    var pricePayload = JsonSerializer.Deserialize<MarketPriceAlertPayload>(message.Body, JsonOptions.Default);
                    if (pricePayload is null)
                    {
                        _logger.LogError("[MessageProcessor] MarketPriceAlert body could not deserialize. ACKing poison message. MessageId={MessageId}", message.MessageId);
                        return true;
                    }
                    _backgroundJobs.Enqueue<IPriceAlertHandler>(h => h.HandleAsync(pricePayload, CancellationToken.None));
                    return true;

                case string msgType when msgType.Equals(EventTypes.StockLowAlert, StringComparison.OrdinalIgnoreCase):
                    var stockLowPayload = JsonSerializer.Deserialize<StockLowAlertPayload>(message.Body, JsonOptions.Default);
                    if (stockLowPayload is null)
                    {
                        _logger.LogError("[MessageProcessor] StockLowAlert body could not deserialize. ACKing poison message. MessageId={MessageId}", message.MessageId);
                        return true;
                    }
                    _backgroundJobs.Enqueue<IStockLowHandler>(h => h.HandleAsync(stockLowPayload, CancellationToken.None));
                    return true;

                case string msgType when msgType.Equals(EventTypes.CompanyNewsAlert, StringComparison.OrdinalIgnoreCase):
                    var newsPayload = JsonSerializer.Deserialize<CompanyNewsAlertPayload>(message.Body, JsonOptions.Default);
                    if (newsPayload is null)
                    {
                        _logger.LogError("[MessageProcessor] CompanyNewsAlert body could not deserialize. ACKing poison message. MessageId={MessageId}", message.MessageId);
                        return true;
                    }
                    _backgroundJobs.Enqueue<INewsHandler>(h => h.HandleAsync(newsPayload, CancellationToken.None));
                    return true;

                default:
                    // Route unknown messages to Hangfire for durability (not in-memory queue which
                    // loses messages on process crash before completion).
                    _logger.LogInformation("[MessageProcessor] Unknown MessageType={MessageType}. Routing to Hangfire default queue.", messageType);
                    await _rawHandler.HandleAsync(message, ct);
                    return true;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "[MessageProcessor] Failed to deserialize payload for type {MessageType}.", messageType);
            return true; // ACK bad JSON to avoid poison message loop
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MessageProcessor] Unexpected error routing message.");
            return false; // Do NOT ACK, allow redelivery
        }
    }

    // IMessageProcessor now declares ProcessAndAcknowledgeAsync — no dual-signature smell.
    Task IMessageProcessor.ProcessMessageAsync(Message message, CancellationToken ct)
        => ProcessAndAcknowledgeAsync(message, ct);
}
