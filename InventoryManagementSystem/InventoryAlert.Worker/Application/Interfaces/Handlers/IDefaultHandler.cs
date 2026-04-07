using Amazon.SQS.Model;
using InventoryAlert.Contracts.Events.Payloads;

namespace InventoryAlert.Worker.Application.Interfaces.Handlers;

/// <summary>
/// The generic handler interface used for ALL payloads.
/// Works interchangeably with Hangfire and BackgroundTaskQueue.
/// </summary>
public interface IDefaultHandler<in TPayload>
{
    Task HandleAsync(TPayload payload, CancellationToken ct = default);
}

/// <summary>
/// The specific interface for raw SQS messages (Unknown types).
/// </summary>
public interface IRawDefaultHandler : IDefaultHandler<Message> { }

/// <summary>Handler for Market Price Alerts.</summary>
public interface IPriceAlertHandler : IDefaultHandler<MarketPriceAlertPayload> { }

/// <summary>Handler for Stock Low Alerts.</summary>
public interface IStockLowHandler : IDefaultHandler<StockLowAlertPayload> { }

/// <summary>Handler for Company News Alerts.</summary>
public interface INewsHandler : IDefaultHandler<CompanyNewsAlertPayload> { }
