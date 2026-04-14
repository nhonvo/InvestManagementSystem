using System.Security.Claims;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAlert.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class PortfolioController(IPortfolioService portfolioService) : ControllerBase
{
    private readonly IPortfolioService _portfolioService = portfolioService;

    [HttpGet("positions")]
    public async Task<ActionResult<PagedResult<PortfolioPositionResponse>>> GetPositions([FromQuery] PortfolioQueryParams query, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var res = await _portfolioService.GetPositionsPagedAsync(query, userId, ct);
        return Ok(res);
    }

    [HttpGet("positions/{symbol}")]
    public async Task<ActionResult<PortfolioPositionResponse>> GetPosition(string symbol, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var res = await _portfolioService.GetPositionBySymbolAsync(symbol, userId, ct);
        return res != null ? Ok(res) : NotFound();
    }

    [HttpPost("positions")]
    public async Task<ActionResult<PortfolioPositionResponse>> OpenPosition([FromBody] CreatePositionRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var res = await _portfolioService.OpenPositionAsync(request, userId, ct);
        return CreatedAtAction(nameof(GetPosition), new { symbol = res.Symbol }, res);
    }

    [HttpPost("bulk")]
    public async Task<ActionResult> BulkImport([FromBody] IEnumerable<CreatePositionRequest> requests, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _portfolioService.BulkImportPositionsAsync(requests, userId, ct);
        return Ok(new { Message = "Bulk import processed." });
    }

    [HttpPost("{symbol}/trades")]
    public async Task<ActionResult<PortfolioPositionResponse>> RecordTrade(string symbol, [FromBody] TradeRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var res = await _portfolioService.RecordTradeAsync(symbol, request, userId, ct);
        return Ok(res);
    }

    [HttpDelete("positions/{symbol}")]
    public async Task<IActionResult> RemovePosition(string symbol, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _portfolioService.RemovePositionAsync(symbol, userId, ct);
        return NoContent();
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<IEnumerable<PortfolioAlertResponse>>> GetAlerts(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var res = await _portfolioService.GetPortfolioAlertsAsync(userId, ct);
        return Ok(res);
    }
}
