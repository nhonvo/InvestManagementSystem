using FluentAssertions;
using InventoryAlert.Api.Controllers;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Web.Controllers;

public class StocksControllerTests
{
    private readonly Mock<IStockDataService> _stockDataService = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IEventService> _eventService = new();
    private readonly StocksController _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public StocksControllerTests()
    {
        _sut = new StocksController(_stockDataService.Object, _uow.Object, _eventService.Object);
    }

    [Fact]
    public async Task GetCatalog_ReturnsPagedResult_WithLocalData()
    {
        // Arrange
        var listings = new List<StockListing>
        {
            new() { TickerSymbol = "AAPL", Name = "Apple", Exchange = "NASDAQ", Industry = "Tech" },
            new() { TickerSymbol = "MSFT", Name = "Microsoft", Exchange = "NASDAQ", Industry = "Tech" }
        };
        _uow.Setup(u => u.StockListings.GetAllAsync(Ct)).ReturnsAsync(listings);

        // Act
        var result = await _sut.GetCatalog(1, 10, null, null, Ct);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var paged = okResult!.Value as PagedResult<StockProfileResponse>;
        paged.Should().NotBeNull();
        paged!.Items.Should().HaveCount(2);
        paged.TotalItems.Should().Be(2);
    }

    [Fact]
    public async Task GetCatalog_FiltersByExchange()
    {
        // Arrange
        var listings = new List<StockListing>
        {
            new() { TickerSymbol = "AAPL", Name = "Apple", Exchange = "NASDAQ" },
            new() { TickerSymbol = "IBM", Name = "IBM", Exchange = "NYSE" }
        };
        _uow.Setup(u => u.StockListings.GetAllAsync(Ct)).ReturnsAsync(listings);

        // Act
        var result = await _sut.GetCatalog(1, 10, "NYSE", null, Ct);

        // Assert
        var okResult = result.Result as OkObjectResult;
        var paged = okResult!.Value as PagedResult<StockProfileResponse>;
        paged!.Items.Should().HaveCount(1);
        paged.Items.First().Symbol.Should().Be("IBM");
    }

    [Fact]
    public async Task TriggerSync_ReturnsAccepted_ForAdmin()
    {
        // Act
        var result = await _sut.TriggerSync(Ct);

        // Assert
        result.Should().BeOfType<AcceptedResult>();
    }
}
