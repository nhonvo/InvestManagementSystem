using System.Security.Claims;
using FluentAssertions;
using InventoryAlert.Api.Controllers;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Web.Controllers;

public class WatchlistControllerTests
{
    private readonly Mock<IWatchlistService> _watchlistService = new();
    private readonly WatchlistController _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;
    private const string UserId = "00000000-0000-0000-0000-000000000001";

    public WatchlistControllerTests()
    {
        _sut = new WatchlistController(_watchlistService.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, UserId)
        }));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetWatchlist_ReturnsOk_WithItems()
    {
        // Arrange
        var items = new List<PortfolioPositionResponse>
        {
            new(1, "AAPL", "Apple", "NASDAQ", null, 0, 0, 150m, 0, 0, 0, 0.5, 1m, 0.5m, "Tech")
        };
        _watchlistService.Setup(s => s.GetWatchlistAsync(UserId, Ct)).ReturnsAsync(items);

        // Act
        var result = await _sut.GetWatchlist(Ct);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().Be(items);
    }

    [Fact]
    public async Task AddToWatchlist_ReturnsCreatedAt_WithResponse()
    {
        // Arrange
        var symbol = "AAPL";
        var expectedRes = new PortfolioPositionResponse(1, symbol, "Apple", "NASDAQ", null, 0, 0, 150m, 0, 0, 0, 0.5, 1m, 0.5m, "Tech");
        _watchlistService.Setup(s => s.AddToWatchlistAsync(symbol, UserId, Ct)).ReturnsAsync(expectedRes);

        // Act
        var result = await _sut.AddToWatchlist(symbol, Ct);

        // Assert
        var crResult = result.Result as CreatedAtActionResult;
        crResult.Should().NotBeNull();
        crResult!.ActionName.Should().Be(nameof(WatchlistController.GetWatchlistItem));
        crResult.Value.Should().Be(expectedRes);
    }

    [Fact]
    public async Task RemoveFromWatchlist_ReturnsOk()
    {
        // Arrange
        var symbol = "AAPL";

        // Act
        var result = await _sut.RemoveFromWatchlist(symbol, Ct);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _watchlistService.Verify(s => s.RemoveFromWatchlistAsync(symbol, UserId, Ct), Times.Once);
    }
}
