using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Contracts.Events;
using InventoryAlert.Contracts.Events.Payloads;
using InventoryAlert.Contracts.Persistence.Entities;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAlert.Api.Web.Controllers;

/// <summary>
/// Accepts external events, triggers manual alerts, and exposes supported event types.
/// All business logic is delegated to IEventService (thin controller rule).
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class EventsController(IEventService eventService) : ControllerBase
{
    private readonly IEventService _eventService = eventService;

    /// <summary>Receive a generic event and publish to SNS.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PublishEvent(
        [FromBody] PublishEventRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.EventType))
            return BadRequest("EventType is required.");

        if (!InventoryAlert.Contracts.Events.EventTypes.IsKnown(request.EventType))
            return BadRequest($"Unknown event type: '{request.EventType}'.");

        await _eventService.PublishEventAsync(request.EventType, request.Payload, ct);
        return Accepted();
    }

    /// <summary>Manually trigger a MarketPriceAlert check on the Worker.</summary>
    [HttpPost("market-alert")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> TriggerMarketAlert(
        [FromBody] MarketAlertRequest request,
        CancellationToken ct)
    {
        var payload = new MarketPriceAlertPayload
        {
            ProductId = request.ProductId,
            Symbol = request.Symbol
        };

        await _eventService.PublishEventAsync(EventTypes.MarketPriceAlert, payload, ct);
        return Accepted();
    }

    /// <summary>Manually trigger a CompanyNewsAlert sync on the Worker.</summary>
    [HttpPost("news-alert")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> TriggerNewsAlert(
        [FromBody] NewsAlertRequest request,
        CancellationToken ct)
    {
        var payload = new CompanyNewsAlertPayload
        {
            Symbol = request.Symbol
        };

        await _eventService.PublishEventAsync(EventTypes.CompanyNewsAlert, payload, ct);
        return Accepted();
    }

    /// <summary>List all supported event types.</summary>
    [HttpGet("types")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventTypes(CancellationToken ct)
    {
        var types = await _eventService.GetSupportedEventTypesAsync();
        return Ok(types);
    }

    /// <summary>Get event logs for a specific event type (from DynamoDB).</summary>
    [HttpGet("logs/{eventType}")]
    [ProducesResponseType(typeof(IEnumerable<EventLogEntry>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventLogs(string eventType, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var logs = await _eventService.GetEventLogsAsync(eventType, limit, ct);
        return Ok(logs);
    }
}
