using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using InventoryAlert.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAlert.Api.Web.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
[ApiController]
[Authorize]
public class StocksController(IStockDataService service) : ControllerBase
{
    private readonly IStockDataService _service = service;

    // ── Symbol search ─────────────────────────────────────────────────────────

    [HttpGet("symbols/search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchSymbols([FromQuery] string q, [FromQuery] string? type, CancellationToken ct)
        => Ok(await _service.SearchSymbolsAsync(q, type, ct));

    // ── Stock data ────────────────────────────────────────────────────────────

    [HttpGet("stocks/{symbol}/quote")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuote(string symbol, CancellationToken ct)
    {
        var result = await _service.GetQuoteAsync(symbol.ToUpperInvariant(), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("stocks/{symbol}/profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(string symbol, CancellationToken ct)
    {
        var result = await _service.GetProfileAsync(symbol.ToUpperInvariant(), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("stocks/{symbol}/news")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCompanyNews(string symbol, [FromQuery][Range(1, 100)] int limit = 10,
        [FromQuery] string? from = null, [FromQuery] string? to = null, CancellationToken ct = default)
    {
        // Simple validation for dates if provided
        if (!string.IsNullOrEmpty(from) && !DateTime.TryParse(from, out _))
            return BadRequest("Invalid 'from' date format. Use YYYY-MM-DD.");
        if (!string.IsNullOrEmpty(to) && !DateTime.TryParse(to, out _))
            return BadRequest("Invalid 'to' date format. Use YYYY-MM-DD.");

        return Ok(await _service.GetCompanyNewsAsync(symbol.ToUpperInvariant(), limit, from, to, ct));
    }

    [HttpGet("stocks/{symbol}/recommendations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecommendations(string symbol, CancellationToken ct)
        => Ok(await _service.GetRecommendationsAsync(symbol.ToUpperInvariant(), ct));

    [HttpGet("stocks/{symbol}/earnings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEarnings(string symbol, [FromQuery] int limit = 4, CancellationToken ct = default)
        => Ok(await _service.GetEarningsAsync(symbol.ToUpperInvariant(), limit, ct));

    [HttpGet("stocks/{symbol}/peers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPeers(string symbol, CancellationToken ct)
        => Ok(await _service.GetPeersAsync(symbol.ToUpperInvariant(), ct));

    // ── Market data ───────────────────────────────────────────────────────────

    [HttpGet("market/news")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMarketNews([FromQuery] string category = "general", [FromQuery] int limit = 20, CancellationToken ct = default)
        => Ok(await _service.GetMarketNewsAsync(category, limit, ct));

    [HttpGet("market/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMarketStatus([FromQuery] string exchange = "US", CancellationToken ct = default)
    {
        var result = await _service.GetMarketStatusAsync(exchange, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("market/holidays")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMarketHolidays([FromQuery] string exchange = "US", CancellationToken ct = default)
        => Ok(await _service.GetMarketHolidaysAsync(exchange, ct));

    [HttpGet("market/earnings-calendar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEarningsCalendar([FromQuery] string? from, [FromQuery] string? to, CancellationToken ct = default)
    {
        var fromDate = from ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
        var toDate = to ?? DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd");
        return Ok(await _service.GetEarningsCalendarAsync(fromDate, toDate, ct));
    }

    // ── Crypto ────────────────────────────────────────────────────────────────

    [HttpGet("crypto/exchanges")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCryptoExchanges(CancellationToken ct)
        => Ok(await _service.GetCryptoExchangesAsync(ct));

    [HttpGet("crypto/symbols")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCryptoSymbols([FromQuery][Required] string exchange, CancellationToken ct)
        => Ok(await _service.GetCryptoSymbolsAsync(exchange, ct));

    [HttpGet("crypto/{symbol}/quote")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCryptoQuote(string symbol, CancellationToken ct)
    {
        var result = await _service.GetCryptoQuoteAsync(symbol, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
