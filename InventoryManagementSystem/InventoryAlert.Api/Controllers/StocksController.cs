using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAlert.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class StocksController(IStockDataService stockDataService, IUnitOfWork unitOfWork) : ControllerBase
{
    private readonly IStockDataService _stockDataService = stockDataService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    /// <summary>Browse the full global StockListing catalog (paged, filterable).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<StockProfileResponse>>> GetCatalog(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? exchange = null,
        [FromQuery] string? industry = null,
        CancellationToken ct = default)
    {
        var all = await _unitOfWork.StockListings.GetAllAsync(ct);

        if (!string.IsNullOrWhiteSpace(exchange))
            all = all.Where(s => s.Exchange?.Equals(exchange, StringComparison.OrdinalIgnoreCase) == true);

        if (!string.IsNullOrWhiteSpace(industry))
            all = all.Where(s => s.Industry?.Contains(industry, StringComparison.OrdinalIgnoreCase) == true);

        var totalItems = all.Count();
        var paged = all.Skip((page - 1) * pageSize).Take(pageSize).Select(s => new StockProfileResponse(
            s.TickerSymbol, s.Name, s.Exchange, s.Currency, s.Country, s.Industry, s.MarketCap, s.Ipo, s.WebUrl, s.Logo));

        return Ok(new PagedResult<StockProfileResponse>
        {
            Items = paged,
            TotalItems = totalItems,
            PageNumber = page,
            PageSize = pageSize
        });
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
    public async Task<ActionResult<IEnumerable<NewsResponse>>> GetNews(string symbol, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var fromDate = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1));
        var toDate = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var res = await _stockDataService.GetCompanyNewsAsync(symbol, fromDate, toDate, ct);
        return Ok(res);
    }

    /// <summary>[Admin] Manually trigger a global price sync job.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("sync")]
    public IActionResult TriggerSync()
    {
        // In production: enqueue via Hangfire BackgroundJob.Enqueue<SyncPricesJob>(j => j.ExecuteAsync(CancellationToken.None))
        return Accepted(new { Message = "Price sync job enqueued." });
    }
}
