using Asp.Versioning;
using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAlert.Api.Web.Controllers;

/// <summary>
/// Accepts external events, triggers manual alerts, and exposes supported event types.
/// All business logic is delegated to IEventService (thin controller rule).
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize]
public class EventsController(IEventService eventService) : ControllerBase
{
    private readonly IEventService _eventService = eventService;

    /// <summary>Receive a generic event and publish to SNS.</summary>
    /// <param name="request">The event to publish.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>202 Accepted if published successfully.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PublishEvent(
        [FromBody] PublishEventRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.EventType))
        {
            return BadRequest(new { Message = "EventType is required." });
        }
        await _eventService.PublishEventAsync(request.EventType, request.Payload, ct);
        return Accepted();
    }

    /// <summary>Manually trigger a MarketPriceAlert check on the Worker.</summary>
    /// <param name="request">Product and symbol to alert on.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("market-alert")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TriggerMarketAlert(
        [FromBody] MarketAlertRequest request,
        CancellationToken ct)
    {
        await _eventService.TriggerMarketAlertAsync(request, ct);
        return Accepted();
    }

    /// <summary>Manually trigger a CompanyNewsAlert sync on the Worker.</summary>
    /// <param name="request">Symbol to alert on.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("news-alert")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TriggerNewsAlert(
        [FromBody] NewsAlertRequest request,
        CancellationToken ct)
    {
        await _eventService.TriggerNewsAlertAsync(request, ct);
        return Accepted();
    }

    /// <summary>List all supported event types.</summary>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("types")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetEventTypes(CancellationToken ct)
    {
        var types = await _eventService.GetSupportedEventTypesAsync();
        return Ok(types);
    }

    /// <summary>Get event logs for a specific event type (from DynamoDB).</summary>
    /// <param name="eventType">The canonical event type string.</param>
    /// <param name="limit">Number of records to fetch.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("logs/{eventType}")]
    [ProducesResponseType(typeof(IEnumerable<EventLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEventLogs(string eventType, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var logs = await _eventService.GetEventLogsAsync(eventType, limit, ct);
        return Ok(logs);
    }
}
