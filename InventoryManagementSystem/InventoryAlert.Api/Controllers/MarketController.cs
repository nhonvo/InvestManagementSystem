using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAlert.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class MarketController(IStockDataService stockDataService) : ControllerBase
{
    private readonly IStockDataService _stockDataService = stockDataService;

    [AllowAnonymous]
    [HttpGet("status")]
    public async Task<ActionResult<IEnumerable<MarketStatusResponse>>> GetStatus(CancellationToken ct)
    {
        var res = await _stockDataService.GetMarketStatusAsync(ct);
        return Ok(res);
    }

    [HttpGet("news")]
    public async Task<ActionResult<IEnumerable<NewsResponse>>> GetNews([FromQuery] string category = "general", [FromQuery] int page = 1, CancellationToken ct = default)
    {
        var res = await _stockDataService.GetMarketNewsAsync(category, page, ct: ct);
        return Ok(res);
    }

    [HttpGet("holiday")]
    public async Task<ActionResult<IEnumerable<MarketHolidayResponse>>> GetHolidays([FromQuery] string exchange, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(exchange)) return BadRequest("Exchange is required.");
        var res = await _stockDataService.GetMarketHolidaysAsync(exchange, ct);
        return Ok(res);
    }

    [HttpGet("calendar/earnings")]
    public async Task<ActionResult<IEnumerable<EarningsCalendarResponse>>> GetEarningsCalendar([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? symbol, CancellationToken ct)
    {
        var fromDate = from ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var toDate = to ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1));
        var res = await _stockDataService.GetEarningsCalendarAsync(fromDate, toDate, symbol, ct);
        return Ok(res);
    }

    [HttpGet("calendar/ipo")]
    public async Task<ActionResult<IEnumerable<IpoCalendarResponse>>> GetIpoCalendar([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var fromDate = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1));
        var toDate = to ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1));
        var res = await _stockDataService.GetIpoCalendarAsync(fromDate, toDate, ct);
        return Ok(res);
    }
}
