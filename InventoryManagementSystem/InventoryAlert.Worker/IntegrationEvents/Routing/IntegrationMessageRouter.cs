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

public class IntegrationMessageRouter(
    IRawDefaultHandler rawHandler,
    IBackgroundJobClient backgroundJobs,
    ILogger<IntegrationMessageRouter> logger) : IIntegrationMessageRouter
{
    private readonly IRawDefaultHandler _rawHandler = rawHandler;
    private readonly IBackgroundJobClient _backgroundJobs = backgroundJobs;
    private readonly ILogger<IntegrationMessageRouter> _logger = logger;

    public async Task<bool> ProcessAndAcknowledgeAsync(Message message, CancellationToken ct)
    {
        if (message.MessageAttributes == null || !message.MessageAttributes.TryGetValue("MessageType", out var attr))
        {
            _logger.LogWarning("[MessageProcessor] Message {MessageId} missing 'MessageType' attribute.", message.MessageId);
            return true;
        }

        var messageType = attr.StringValue;

        try
        {
            switch (messageType)
            {
                case EventTypes.StockLowAlert:
                    var payload = JsonSerializer.Deserialize<LowHoldingsAlertPayload>(message.Body, JsonOptions.Default);
                    if (payload != null)
                    {
                        _backgroundJobs.Enqueue<LowHoldingsHandler>(h => h.HandleAsync(payload, CancellationToken.None));
                    }
                    return true;

                case EventTypes.MarketPriceAlert:
                    var pricePayload = JsonSerializer.Deserialize<MarketPriceAlertPayload>(message.Body, JsonOptions.Default);
                    if (pricePayload != null)
                    {
                        _backgroundJobs.Enqueue<MarketPriceAlertHandler>(h => h.HandleAsync(pricePayload, CancellationToken.None));
                    }
                    return true;

                case EventTypes.CompanyNewsAlert:
                    var newsPayload = JsonSerializer.Deserialize<CompanyNewsAlertPayload>(message.Body, JsonOptions.Default);
                    if (newsPayload != null)
                    {
                        _backgroundJobs.Enqueue<CompanyNewsAlertHandler>(h => h.HandleAsync(newsPayload, CancellationToken.None));
                    }
                    return true;

                case EventTypes.SyncMarketNewsRequested:
                    _logger.LogInformation("[MessageProcessor] SyncMarketNewsRequested event received. Enqueuing NewsSyncJob.");
                    _backgroundJobs.Enqueue<NewsSyncJob>(job => job.ExecuteAsync(CancellationToken.None));
                    return true;
                
                case EventTypes.SyncCompanyNewsRequested:
                    var syncPayload = JsonSerializer.Deserialize<CompanyNewsAlertPayload>(message.Body, JsonOptions.Default);
                    if (syncPayload != null)
                    {
                        _logger.LogInformation("[MessageProcessor] SyncCompanyNewsRequested for {Symbol} received.", syncPayload.Symbol);
                        _backgroundJobs.Enqueue<CompanyNewsAlertHandler>(h => h.HandleAsync(syncPayload, CancellationToken.None));
                    }
                    return true;

                default:
                    _logger.LogInformation("[MessageProcessor] Unhandled MessageType={MessageType}. Finalizing raw processing.", messageType);
                    await _rawHandler.HandleAsync(message, ct);
                    return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MessageProcessor] Failed to route message {MessageType}.", messageType);
            return false;
        }
    }

    public Task ProcessMessageAsync(Message message, CancellationToken ct)
        => ProcessAndAcknowledgeAsync(message, ct);
}
