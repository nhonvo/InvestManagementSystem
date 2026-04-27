using System.Text.Json;
using Amazon.SQS.Model;
using Hangfire;
using InventoryAlert.Domain.Configuration;
using InventoryAlert.Domain.Events;
using InventoryAlert.Domain.Events.Payloads;
using InventoryAlert.Worker.IntegrationEvents.Handlers;
using InventoryAlert.Worker.Interfaces;
using InventoryAlert.Worker.ScheduledJobs;

namespace InventoryAlert.Worker.IntegrationEvents.Routing;

/// <summary>
/// Routes EventEnvelopes to their respective Handlers or Background Jobs.
/// Standardizes on the EventEnvelope contract for all cross-service communication.
/// </summary>
public class IntegrationMessageRouter(
    IRawDefaultHandler rawHandler,
    IBackgroundJobClient backgroundJobs,
    ILogger<IntegrationMessageRouter> logger) : IIntegrationMessageRouter
{
    private readonly IRawDefaultHandler _rawHandler = rawHandler;
    private readonly IBackgroundJobClient _backgroundJobs = backgroundJobs;
    private readonly ILogger<IntegrationMessageRouter> _logger = logger;

    public async Task<bool> RouteEnvelopeAsync(EventEnvelope envelope, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrEmpty(envelope.EventType))
            {
                _logger.LogWarning("[MessageRouter] Received envelope with empty EventType. MessageId: {Id}", envelope.MessageId);
                return true;
            }

            switch (envelope.EventType)
            {
                case EventTypes.StockLowAlert:
                    var lowHoldingsPayload = JsonSerializer.Deserialize<LowHoldingsAlertPayload>(envelope.Payload, JsonOptions.Default);
                    if (lowHoldingsPayload != null)
                    {
                        _backgroundJobs.Enqueue<LowHoldingsHandler>(h => h.HandleAsync(lowHoldingsPayload, CancellationToken.None));
                    }
                    return true;

                case EventTypes.MarketPriceAlert:
                    var pricePayload = JsonSerializer.Deserialize<MarketPriceAlertPayload>(envelope.Payload, JsonOptions.Default);
                    if (pricePayload != null)
                    {
                        _backgroundJobs.Enqueue<MarketPriceAlertHandler>(h => h.HandleAsync(pricePayload, CancellationToken.None));
                    }
                    return true;

                case EventTypes.SyncMarketNewsRequested:
                    _logger.LogInformation("[MessageRouter] SyncMarketNewsRequested received. Enqueuing NewsSyncJob.");
                    _backgroundJobs.Enqueue<NewsSyncJob>(job => job.ExecuteAsync(CancellationToken.None));
                    return true;
                
                case EventTypes.SyncCompanyNewsRequested:
                    var syncNewsPayload = JsonSerializer.Deserialize<CompanyNewsAlertPayload>(envelope.Payload, JsonOptions.Default);
                    if (syncNewsPayload != null)
                    {
                        _logger.LogInformation("[MessageRouter] SyncCompanyNewsRequested for {Symbol} received.", syncNewsPayload.Symbol);
                        // Consolidate: Send to NewsSyncJob if logic is shared, or keep as CompanyNewsAlertHandler
                        _backgroundJobs.Enqueue<NewsSyncJob>(job => job.ExecuteAsync(CancellationToken.None));
                    }
                    return true;

                case EventTypes.TestFailureRequested:
                    _logger.LogCritical("[MessageRouter] TestFailureRequested received. Throwing for retry testing.");
                    throw new InvalidOperationException("E2E Test Poison Message Failure");

                default:
                    _logger.LogInformation("[MessageRouter] Unhandled EventType={EventType}. MessageId: {Id}", envelope.EventType, envelope.MessageId);
                    return true; // ACK unknown events
            }
        }
        catch (InvalidOperationException) when (envelope.EventType == EventTypes.TestFailureRequested)
        {
            throw; // Re-throw for E2E testing
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MessageRouter] Failed to route EventType {EventType}.", envelope.EventType);
            return false;
        }
    }

    public async Task<bool> ProcessAndAcknowledgeAsync(Message message, CancellationToken ct)
    {
        try
        {
            string body = message.Body;

            // 1. Detect if it's an SNS-wrapped message (contains "Message" and "Type")
            using var doc = JsonDocument.Parse(message.Body);
            if (doc.RootElement.TryGetProperty("Message", out var innerMsg) && doc.RootElement.TryGetProperty("Type", out var type) && type.GetString() == "Notification")
            {
                _logger.LogDebug("[MessageRouter] Detected SNS wrap for message {MessageId}. Unwrapping.", message.MessageId);
                body = innerMsg.GetString() ?? message.Body;
            }

            // 2. Deserialize Envelope
            var envelope = JsonSerializer.Deserialize<EventEnvelope>(body, JsonOptions.Default);
            if (envelope == null || string.IsNullOrEmpty(envelope.EventType))
            {
                _logger.LogWarning("[MessageRouter] Message {MessageId} is not a valid EventEnvelope. Delegating to raw handler.", message.MessageId);
                await _rawHandler.HandleAsync(message, ct);
                return true;
            }

            return await RouteEnvelopeAsync(envelope, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MessageRouter] Critical failure processing message {MessageId}.", message.MessageId);
            return false;
        }
    }

    public Task ProcessMessageAsync(Message message, CancellationToken ct)
        => ProcessAndAcknowledgeAsync(message, ct);
}
