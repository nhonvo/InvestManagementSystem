using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAlert.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class EventsController(IEventService eventService) : ControllerBase
{
    private readonly IEventService _eventService = eventService;

    [HttpPost]
    public async Task<ActionResult> PublishEvent([FromBody] PublishEventRequest request, CancellationToken ct)
    {
        await _eventService.PublishEventAsync(request.EventType, request.Payload, ct);
        return Accepted(new { Status = "Queued" });
    }

    [HttpGet("types")]
    public async Task<ActionResult<IEnumerable<string>>> GetEventTypes(CancellationToken ct)
    {
        var types = await _eventService.GetSupportedEventTypesAsync();
        return Ok(types);
    }
}
