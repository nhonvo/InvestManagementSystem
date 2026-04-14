using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAlert.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class NewsController(IStockDataService stockDataService) : ControllerBase
{

    // TODO: OUTDATE REMOVE IT 
    private readonly IStockDataService _stockDataService = stockDataService;

    [HttpGet("market")]
    public async Task<ActionResult<IEnumerable<NewsResponse>>> GetMarketNews([FromQuery] string category = "general", [FromQuery] int page = 1, CancellationToken ct = default)
    {
        var res = await _stockDataService.GetMarketNewsAsync(category, page, ct);
        return Ok(res);
    }


    [HttpGet("company/{symbol}")]
    public async Task<ActionResult<IEnumerable<NewsResponse>>> GetCompanyNews(string symbol, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct = default)
    {
        var fromDate = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));
        var toDate = to ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var res = await _stockDataService.GetCompanyNewsAsync(symbol, fromDate, toDate, ct);
        return Ok(res);
    }
}
