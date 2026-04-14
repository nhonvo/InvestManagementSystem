using FluentAssertions;
using InventoryAlert.Api.Services;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Application.Services;

public class PortfolioServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IStockDataService> _stockData = new();
    private readonly Mock<ILogger<PortfolioService>> _logger = new();
    private readonly PortfolioService _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;
    private const string UserId = "00000000-0000-0000-0000-000000000001";
    private static readonly Guid UserGuid = Guid.Parse(UserId);

    public PortfolioServiceTests()
    {
        _sut = new PortfolioService(_uow.Object, _stockData.Object, _logger.Object);

        // Standard mock to invoke the transaction delegate
        _uow.Setup(u => u.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((action, _) => action());
    }

    [Fact]
    public async Task GetPosition_CalculatesHoldingsCorrectly_FromMultipleTrades()
    {
        // Arrange
        var symbol = "AAPL";
        var listing = new StockListing { Id = 1, TickerSymbol = symbol, Name = "Apple" };
        var trades = new List<Trade>
        {
            new() { Type = TradeType.Buy, Quantity = 10, UnitPrice = 150m, TickerSymbol = symbol },
            new() { Type = TradeType.Buy, Quantity = 5, UnitPrice = 160m, TickerSymbol = symbol },
            new() { Type = TradeType.Sell, Quantity = 3, UnitPrice = 170m, TickerSymbol = symbol }
        };

        _uow.Setup(u => u.Trades.GetByUserAndSymbolAsync(UserGuid, symbol, Ct)).ReturnsAsync(trades);
        _uow.Setup(u => u.StockListings.FindBySymbolAsync(symbol, Ct)).ReturnsAsync(listing);
        _stockData.Setup(s => s.GetQuoteAsync(symbol, Ct))
            .ReturnsAsync(new StockQuoteResponse(symbol, 180m, 2m, 1.1, 182m, 178m, 179m, 178m, DateTime.UtcNow));

        // Act
        var result = await _sut.GetPositionBySymbolAsync(symbol, UserId, Ct);

        // Assert
        result.Should().NotBeNull();
        result!.HoldingsCount.Should().Be(12); // (10 + 5) - 3
        result.AveragePrice.Should().Be(153.33333333333333333333333333m); // (10*150 + 5*160) / 15
        result.CurrentPrice.Should().Be(180m);
        result.MarketValue.Should().Be(12 * 180m);
        result.TotalReturn.Should().Be((12 * 180m) - (12 * 153.33333333333333333333333333m));
    }

    [Fact]
    public async Task RecordTrade_Throws_WhenSellingMoreThanOwned()
    {
        // Arrange
        var symbol = "MSFT";
        var request = new TradeRequest(TradeType.Sell, 50, 400m, "Selling too much");

        _uow.Setup(u => u.Trades.GetNetHoldingsAsync(UserGuid, symbol, Ct)).ReturnsAsync(10m);

        // Act
        var act = () => _sut.RecordTradeAsync(symbol, request, UserId, Ct);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Insufficient holdings*");
    }

    [Fact]
    public async Task OpenPosition_EnsuresListingExists_BeforeAdding()
    {
        // Arrange
        var request = new CreatePositionRequest("INVALID", 10, 100m, null);
        _uow.Setup(u => u.StockListings.FindBySymbolAsync("INVALID", Ct)).ReturnsAsync((StockListing?)null);

        // Act
        var act = () => _sut.OpenPositionAsync(request, UserId, Ct);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*must be resolved before opening a position.*");
    }

    [Fact]
    public async Task RemovePosition_Throws_IfActiveAlertsExist()
    {
        // Arrange
        var symbol = "TSLA";
        var activeRules = new List<AlertRule> {
            new() { TickerSymbol = symbol, IsActive = true }
        };
        _uow.Setup(u => u.AlertRules.GetByUserIdAsync(UserId, Ct)).ReturnsAsync(activeRules);

        // Act
        var act = () => _sut.RemovePositionAsync(symbol, UserId, Ct);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot remove a position with active alert rules.*");
    }
}
