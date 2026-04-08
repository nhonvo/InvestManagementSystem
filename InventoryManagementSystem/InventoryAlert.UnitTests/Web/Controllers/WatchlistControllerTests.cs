using FluentAssertions;
using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace InventoryAlert.UnitTests.Web.Controllers;

public class WatchlistControllerTests
{
    private readonly Mock<IWatchlistService> _service = new();
    private readonly WatchlistController _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public WatchlistControllerTests()
    {
        _sut = new WatchlistController(_service.Object);
        
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
            new(ClaimTypes.NameIdentifier, "user-1"),
        }, "mock"));

        _sut.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };
    }

    [Fact]
    public async Task GetWatchlist_Returns200_WithItems()
    {
        var items = new List<WatchlistItemResponse> { 
            new("AAPL", "Apple", "NASDAQ", "Stock", 150m, 1m, 0.5m, DateTime.UtcNow) 
        };
        _service.Setup(s => s.GetUserWatchlistAsync("user-1", Ct)).ReturnsAsync(items);

        var result = await _sut.GetWatchlist(Ct);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().BeEquivalentTo(items);
    }

    [Fact]
    public async Task AddToWatchlist_Returns204_WhenSuccessful()
    {
        _service.Setup(s => s.AddToWatchlistAsync("user-1", "AAPL", Ct)).Returns(Task.CompletedTask);

        var result = await _sut.AddToWatchlist("AAPL", Ct);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RemoveFromWatchlist_Returns204_WhenSuccessful()
    {
        _service.Setup(s => s.RemoveFromWatchlistAsync("user-1", "AAPL", Ct)).Returns(Task.CompletedTask);

        var result = await _sut.RemoveFromWatchlist("AAPL", Ct);

        result.Should().BeOfType<NoContentResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }
}
