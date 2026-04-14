using System.Security.Claims;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAlert.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class AlertRulesController(IAlertRuleService alertRuleService) : ControllerBase
{
    private readonly IAlertRuleService _alertRuleService = alertRuleService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AlertRuleResponse>>> GetAlerts(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var res = await _alertRuleService.GetByUserIdAsync(userId, ct);
        return Ok(res);
    }

    [HttpPost]
    public async Task<ActionResult<AlertRuleResponse>> Create([FromBody] AlertRuleRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var res = await _alertRuleService.CreateAsync(request, userId, ct);
        return CreatedAtAction(nameof(GetAlerts), null, res);
    }

    /// <summary>
    /// Full replacement of an existing alert rule (PUT semantics).
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AlertRuleResponse>> Update(Guid id, [FromBody] AlertRuleRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var res = await _alertRuleService.UpdateAsync(id, request, userId, ct);
        return Ok(res);
    }

    [HttpPatch("{id:guid}/toggle")]
    public async Task<ActionResult<AlertRuleResponse>> Toggle(Guid id, [FromBody] ToggleAlertRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var res = await _alertRuleService.ToggleAsync(id, request.IsActive, userId, ct);
        return Ok(res);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _alertRuleService.DeleteAsync(id, userId, ct);
        return NoContent();
    }
}
