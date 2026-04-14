using FluentAssertions;
using InventoryAlert.Api.Services;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Application.Services;

public class WatchlistServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IStockDataService> _stockData = new();
    private readonly Mock<ILogger<WatchlistService>> _logger = new();
    private readonly WatchlistService _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;
    private const string UserId = "00000000-0000-0000-0000-000000000001";
    private static readonly Guid UserGuid = Guid.Parse(UserId);

    public WatchlistServiceTests()
    {
        _sut = new WatchlistService(_uow.Object, _stockData.Object, _logger.Object);
    }

    [Fact]
    public async Task GetWatchlist_ReturnsPositions_WithZeroHoldings()
    {
        // Arrange
        var items = new List<WatchlistItem>
        {
            new() { TickerSymbol = "AAPL", UserId = UserGuid },
            new() { TickerSymbol = "MSFT", UserId = UserGuid }
        };
        _uow.Setup(u => u.WatchlistItems.GetByUserIdAsync(UserId, Ct)).ReturnsAsync(items);
        _uow.Setup(u => u.StockListings.FindBySymbolAsync("AAPL", Ct)).ReturnsAsync(new StockListing { Id = 1, TickerSymbol = "AAPL", Name = "Apple" });
        _uow.Setup(u => u.StockListings.FindBySymbolAsync("MSFT", Ct)).ReturnsAsync(new StockListing { Id = 2, TickerSymbol = "MSFT", Name = "Microsoft" });
        _stockData.Setup(s => s.GetQuoteAsync(It.IsAny<string>(), Ct))
            .ReturnsAsync(new StockQuoteResponse("TEST", 150m, 1m, 0.5, 155m, 145m, 148m, 147m, DateTime.UtcNow));

        // Act
        var result = await _sut.GetWatchlistAsync(UserId, Ct);

        // Assert
        result.Should().HaveCount(2);
        result.All(r => r.HoldingsCount == 0).Should().BeTrue();
    }

    [Fact]
    public async Task AddToWatchlist_ChecksListingFallback()
    {
        // Arrange
        var symbol = "GOOGL";
        _uow.Setup(u => u.WatchlistItems.GetByUserAndSymbolAsync(UserId, symbol, Ct)).ReturnsAsync((WatchlistItem?)null);
        _uow.Setup(u => u.StockListings.FindBySymbolAsync(symbol, Ct)).ReturnsAsync((StockListing?)null);
        _stockData.Setup(s => s.GetProfileAsync(symbol, Ct)).ReturnsAsync(new StockProfileResponse(symbol, "Google", "NASDAQ", "USD", "US", "Tech", 1000m, null, null, null));
        _uow.Setup(u => u.StockListings.FindBySymbolAsync(symbol, Ct)).ReturnsAsync(new StockListing { Id = 3, TickerSymbol = symbol, Name = "Google" });

        // Act
        var result = await _sut.AddToWatchlistAsync(symbol, UserId, Ct);

        // Assert
        result.Symbol.Should().Be(symbol);
        _uow.Verify(u => u.WatchlistItems.AddAsync(It.Is<WatchlistItem>(w => w.TickerSymbol == symbol), Ct), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }

    [Fact]
    public async Task RemoveFromWatchlist_CallsDelete_WhenItemExists()
    {
        // Arrange
        var symbol = "AAPL";
        var item = new WatchlistItem { TickerSymbol = symbol, UserId = UserGuid };
        _uow.Setup(u => u.WatchlistItems.GetByUserAndSymbolAsync(UserId, symbol, Ct)).ReturnsAsync(item);

        // Act
        await _sut.RemoveFromWatchlistAsync(symbol, UserId, Ct);

        // Assert
        _uow.Verify(u => u.WatchlistItems.DeleteAsync(item, Ct), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }

    [Fact]
    public async Task AddToWatchlist_Throws_IfAlreadyExists()
    {
        // Arrange
        var symbol = "AAPL";
        _uow.Setup(u => u.WatchlistItems.GetByUserAndSymbolAsync(UserId, symbol, Ct)).ReturnsAsync(new WatchlistItem());

        // Act
        var act = () => _sut.AddToWatchlistAsync(symbol, UserId, Ct);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Symbol '{symbol}' is already on your watchlist.");
    }
}
