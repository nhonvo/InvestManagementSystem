using System.Security.Claims;
using InventoryAlert.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAlert.Api.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/watchlist")]
public class WatchlistController(IWatchlistService service) : ControllerBase
{
    private readonly IWatchlistService _service = service;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
        ?? "anonymous";

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWatchlist(CancellationToken ct)
    {
        var result = await _service.GetUserWatchlistAsync(UserId, ct);
        return Ok(result);
    }

    [HttpPost("{symbol}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddToWatchlist(string symbol, CancellationToken ct)
    {
        await _service.AddToWatchlistAsync(UserId, symbol.ToUpperInvariant(), ct);
        return NoContent();
    }

    [HttpDelete("{symbol}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveFromWatchlist(string symbol, CancellationToken ct)
    {
        await _service.RemoveFromWatchlistAsync(UserId, symbol.ToUpperInvariant(), ct);
        return NoContent();
    }
}
