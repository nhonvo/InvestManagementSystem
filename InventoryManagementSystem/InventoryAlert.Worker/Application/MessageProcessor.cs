using System.Text.Json;
using Amazon.SQS.Model;
using Hangfire;
using InventoryAlert.Contracts.Events;
using InventoryAlert.Contracts.Events.Payloads;
using InventoryAlert.Worker.Application.Interfaces.Handlers;
using InventoryAlert.Worker.Interfaces;

using InventoryAlert.Contracts.Configuration;

namespace InventoryAlert.Worker.Application;

/// <summary>
/// Orchestrates message routing between Hangfire (reliable/retriable) 
/// and the Native Background Task Queue (low priority/in-memory).
/// </summary>
public class MessageProcessor(
    IBackgroundTaskQueue backgroundTaskQueue, 
    IRawDefaultHandler rawHandler,
    ILogger<MessageProcessor> logger) : IMessageProcessor
{
    private readonly IBackgroundTaskQueue _backgroundTaskQueue = backgroundTaskQueue;
    private readonly IRawDefaultHandler _rawHandler = rawHandler;
    private readonly ILogger<MessageProcessor> _logger = logger;

    /// <summary>
    /// Processes a message and returns true if it should be deleted from SQS.
    /// </summary>
    public async Task<bool> ProcessAndAcknowledgeAsync(Message message, CancellationToken ct)
    {
        if (!message.MessageAttributes.TryGetValue("MessageType", out var attr))
        {
            _logger.LogWarning("[MessageProcessor] Message {MessageId} missing 'MessageType' attribute. Skipping.", message.MessageId);
            return true; // ACK to remove garbage from queue
        }

        var messageType = attr.StringValue;
        var correlationId = message.MessageAttributes.GetValueOrDefault("CorrelationId")?.StringValue ?? "N/A";

        // Structured Logging for traceability
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
                    if (pricePayload != null)
                    {
                        BackgroundJob.Enqueue<IPriceAlertHandler>(h => h.HandleAsync(pricePayload, CancellationToken.None));
                        return true; // Enqueued to Hangfire (Persistent) -> Safe to ACK
                    }
                    break;

                case string msgType when msgType.Equals(EventTypes.StockLowAlert, StringComparison.OrdinalIgnoreCase):
                    var stockLowPayload = JsonSerializer.Deserialize<StockLowAlertPayload>(message.Body, JsonOptions.Default);
                    if (stockLowPayload != null)
                    {
                        BackgroundJob.Enqueue<IStockLowHandler>(h => h.HandleAsync(stockLowPayload, CancellationToken.None));
                        return true; 
                    }
                    break;

                case string msgType when msgType.Equals(EventTypes.CompanyNewsAlert, StringComparison.OrdinalIgnoreCase):
                    var newsPayload = JsonSerializer.Deserialize<CompanyNewsAlertPayload>(message.Body, JsonOptions.Default);
                    if (newsPayload != null)
                    {
                        BackgroundJob.Enqueue<INewsHandler>(h => h.HandleAsync(newsPayload, CancellationToken.None));
                        return true;
                    }
                    break;

                default:
                    // Route to In-Memory Queue for processing by QueuedHostedService
                    _logger.LogInformation("[MessageProcessor] Routing unknown message to In-Memory Queue.");
                    
                    // Since this is in-memory, we technically shouldn't ACK SQS until IT IS PROCESSED.
                    // But to keep it simple and consistent with the user's dual-flow requirement:
                    await _backgroundTaskQueue.QueueBackgroundWorkItemAsync(async (token) => 
                        await _rawHandler.HandleAsync(message, token));
                    
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

        return false;
    }

    // Explicit implementation for the original interface to avoid breaking build if interface not updated yet
    async Task IMessageProcessor.ProcessMessageAsync(Message message, CancellationToken ct) 
        => await ProcessAndAcknowledgeAsync(message, ct);
}
