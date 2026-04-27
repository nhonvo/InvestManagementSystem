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

    public async Task ProcessMessageAsync(Message message, CancellationToken ct)
    {
        // This is now a legacy bridge if any direct callers remain. 
        // Real logic should flow through RouteEnvelopeAsync.
        _logger.LogWarning("[MessageRouter] ProcessMessageAsync (Legacy) called for Message {Id}.", message.MessageId);
        await _rawHandler.HandleAsync(message, ct);
    }
}
