using FluentAssertions;
using InventoryAlert.Api.Controllers;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Web.Controllers;

public class StocksControllerTests
{
    private readonly Mock<IStockDataService> _stockDataService = new();
    private readonly StocksController _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public StocksControllerTests()
    {
        _sut = new StocksController(_stockDataService.Object);
    }

    [Fact]
    public async Task GetCatalog_ReturnsPagedResult_FromService()
    {
        // Arrange
        var pagedResult = new PagedResult<StockProfileResponse>
        {
            Items = new List<StockProfileResponse>
            {
                new("AAPL", "Apple", "NASDAQ", "USD", "USA", "Tech", null, null, null, null),
                new("MSFT", "Microsoft", "NASDAQ", "USD", "USA", "Tech", null, null, null, null)
            },
            TotalItems = 2,
            PageNumber = 1,
            PageSize = 10
        };
        _stockDataService.Setup(s => s.GetCatalogAsync(1, 10, null, null, Ct)).ReturnsAsync(pagedResult);

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
    public async Task GetCatalog_PassesFiltersToService()
    {
        // Arrange
        var pagedResult = new PagedResult<StockProfileResponse>
        {
            Items = new List<StockProfileResponse>
            {
                new("IBM", "IBM", "NYSE", "USD", "USA", "Tech", null, null, null, null)
            },
            TotalItems = 1,
            PageNumber = 1,
            PageSize = 10
        };
        _stockDataService.Setup(s => s.GetCatalogAsync(1, 10, "NYSE", "Tech", Ct)).ReturnsAsync(pagedResult);

        // Act
        var result = await _sut.GetCatalog(1, 10, "NYSE", "Tech", Ct);

        // Assert
        var okResult = result.Result as OkObjectResult;
        var paged = okResult!.Value as PagedResult<StockProfileResponse>;
        paged!.Items.Should().HaveCount(1);
        paged.Items.First().Symbol.Should().Be("IBM");
    }
}
