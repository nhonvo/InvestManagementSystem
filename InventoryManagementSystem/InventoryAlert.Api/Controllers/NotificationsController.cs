using System.Security.Claims;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAlert.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    private readonly INotificationService _notificationService = notificationService;

    [HttpGet]
    public async Task<ActionResult<PagedResult<NotificationResponse>>> GetNotifications([FromQuery] bool onlyUnread = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var res = await _notificationService.GetPagedAsync(userId, onlyUnread, page, pageSize, ct);
        return Ok(res);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var res = await _notificationService.GetUnreadCountAsync(userId, ct);
        return Ok(res);
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _notificationService.MarkReadAsync(id, userId, ct);
        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<ActionResult<int>> MarkAllRead(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var res = await _notificationService.MarkAllReadAsync(userId, ct);
        return Ok(res);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Dismiss(Guid id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _notificationService.DismissAsync(id, userId, ct);
        return NoContent();
    }
}
