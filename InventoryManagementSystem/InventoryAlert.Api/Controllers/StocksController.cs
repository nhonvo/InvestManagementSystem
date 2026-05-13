using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAlert.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class StocksController(IStockDataService stockDataService) : ControllerBase
{
    private readonly IStockDataService _stockDataService = stockDataService;

    /// <summary>Browse the full global StockListing catalog (paged, filterable).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<StockProfileResponse>>> GetCatalog(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? exchange = null,
        [FromQuery] string? industry = null,
        CancellationToken ct = default)
    {
        var result = await _stockDataService.GetCatalogAsync(page, pageSize, exchange, industry, ct);
        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<SymbolSearchResponse>>> Search([FromQuery] string q, CancellationToken ct)
    {
        var res = await _stockDataService.SearchSymbolsAsync(q, ct);
        return Ok(res);
    }

    [HttpGet("{symbol}/quote")]
    public async Task<ActionResult<StockQuoteResponse>> GetQuote(string symbol, CancellationToken ct)
    {
        var res = await _stockDataService.GetQuoteAsync(symbol, ct);
        return res != null ? Ok(res) : NotFound();
    }

    [HttpGet("{symbol}/profile")]
    public async Task<ActionResult<StockProfileResponse>> GetProfile(string symbol, CancellationToken ct)
    {
        var res = await _stockDataService.GetProfileAsync(symbol, ct);
        return res != null ? Ok(res) : NotFound();
    }

    [HttpGet("{symbol}/financials")]
    public async Task<ActionResult<StockMetricResponse>> GetFinancials(string symbol, CancellationToken ct)
    {
        var res = await _stockDataService.GetFinancialsAsync(symbol, ct);
        return res != null ? Ok(res) : NotFound();
    }

    [HttpGet("{symbol}/earnings")]
    public async Task<ActionResult<IEnumerable<EarningsSurpriseResponse>>> GetEarnings(string symbol, CancellationToken ct)
    {
        var res = await _stockDataService.GetEarningsAsync(symbol, ct);
        return Ok(res);
    }

    [HttpGet("{symbol}/recommendation")]
    public async Task<ActionResult<IEnumerable<RecommendationResponse>>> GetRecommendations(string symbol, CancellationToken ct)
    {
        var res = await _stockDataService.GetRecommendationsAsync(symbol, ct);
        return Ok(res);
    }

    [HttpGet("{symbol}/insiders")]
    public async Task<ActionResult<IEnumerable<InsiderTransactionResponse>>> GetInsiders(string symbol, CancellationToken ct)
    {
        var res = await _stockDataService.GetInsidersAsync(symbol, ct);
        return Ok(res);
    }

    [HttpGet("{symbol}/peers")]
    public async Task<ActionResult<PeersResponse>> GetPeers(string symbol, CancellationToken ct)
    {
        var res = await _stockDataService.GetPeersAsync(symbol, ct);
        return res != null ? Ok(res) : NotFound();
    }
    [HttpGet("{symbol}/news")]
    public async Task<ActionResult<IEnumerable<NewsResponse>>> GetNews(string symbol, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var res = await _stockDataService.GetCompanyNewsAsync(symbol, page, pageSize, ct);
        return Ok(res);
    }
}
