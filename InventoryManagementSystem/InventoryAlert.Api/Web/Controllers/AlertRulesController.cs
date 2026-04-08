using System.Security.Claims;
using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAlert.Api.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/alerts")]
public class AlertRulesController(IAlertRuleService service) : ControllerBase
{
    private readonly IAlertRuleService _service = service;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
        ?? "anonymous";

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAlerts(CancellationToken ct)
        => Ok(await _service.GetUserAlertsAsync(UserId, ct));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAlert([FromBody] AlertRuleRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAlertAsync(UserId, request, ct);
        return CreatedAtAction(nameof(GetAlerts), new { id = result.Id }, result);
    }

    [HttpPut("{ruleId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAlert(Guid ruleId, [FromBody] AlertRuleRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAlertAsync(UserId, ruleId, request, ct));

    [HttpDelete("{ruleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAlert(Guid ruleId, CancellationToken ct)
    {
        await _service.DeleteAlertAsync(UserId, ruleId, ct);
        return NoContent();
    }
}
