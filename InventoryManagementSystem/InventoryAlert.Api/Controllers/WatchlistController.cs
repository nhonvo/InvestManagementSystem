using System.Security.Claims;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAlert.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class WatchlistController(IWatchlistService watchlistService) : ControllerBase
{
    private readonly IWatchlistService _watchlistService = watchlistService;

    /// <summary>List all symbols on the current user's watchlist with live price data.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PortfolioPositionResponse>>> GetWatchlist(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var res = await _watchlistService.GetWatchlistAsync(userId, ct);
        return Ok(res);
    }

    /// <summary>Detailed view of a single watchlist entry.</summary>
    [HttpGet("{symbol}")]
    public async Task<ActionResult<PortfolioPositionResponse>> GetWatchlistItem(string symbol, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var res = await _watchlistService.GetWatchlistItemAsync(symbol, userId, ct);
        return res != null ? Ok(res) : NotFound(new { Message = $"Symbol '{symbol}' is not on your watchlist." });
    }

    /// <summary>Add a ticker to the current user's watchlist. Resolves via DB-first + Finnhub fallback.</summary>
    [HttpPost("{symbol}")]
    public async Task<ActionResult<PortfolioPositionResponse>> AddToWatchlist(string symbol, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var res = await _watchlistService.AddToWatchlistAsync(symbol, userId, ct);
        return CreatedAtAction(nameof(GetWatchlistItem), new { symbol = res.Symbol }, res);
    }

    /// <summary>Remove a ticker from the watchlist.</summary>
    [HttpDelete("{symbol}")]
    public async Task<IActionResult> RemoveFromWatchlist(string symbol, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _watchlistService.RemoveFromWatchlistAsync(symbol, userId, ct);
        return Ok(new { Message = $"'{symbol}' removed from watchlist." });
    }
}
