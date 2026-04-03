using FluentAssertions;
using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Web.Controllers;
using InventoryAlert.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InventoryAlert.Tests.Web.Controllers;

public class ProductsControllerTests
{
    private readonly Mock<IProductService> _service = new();
    private readonly ProductsController _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public ProductsControllerTests()
    {
        _sut = new ProductsController(_service.Object);
    }

    // ════════════════════════════════════════════════════════════════
    // GET api/products
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetProducts_Returns200_WithList()
    {
        var responses = new[]
        {
            ProductFixtures.BuildResponse(id: 1),
            ProductFixtures.BuildResponse(id: 2)
        };
        _service.Setup(s => s.GetAllProductsAsync(Ct)).ReturnsAsync(responses);

        var result = await _sut.GetProducts(Ct);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.As<IEnumerable<ProductResponse>>().Should().HaveCount(2);
    }

    // ════════════════════════════════════════════════════════════════
    // GET api/products/{id}
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetById_Returns200_WhenFound()
    {
        _service.Setup(s => s.GetProductByIdAsync(1, Ct))
            .ReturnsAsync(ProductFixtures.BuildResponse(id: 1));

        var result = await _sut.GetProductById(1, Ct);

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task GetById_Returns404_WhenNotFound()
    {
        _service.Setup(s => s.GetProductByIdAsync(99, Ct))
            .ReturnsAsync((ProductResponse?)null);

        var result = await _sut.GetProductById(99, Ct);

        result.Should().BeOfType<NotFoundResult>();
    }

    // ════════════════════════════════════════════════════════════════
    // POST api/products
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Create_Returns201CreatedAtAction_WithCorrectLocation()
    {
        var request = ProductFixtures.BuildRequest();
        var response = ProductFixtures.BuildResponse(id: 5);
        _service.Setup(s => s.CreateProductAsync(request, Ct)).ReturnsAsync(response);

        var result = await _sut.CreateProduct(request, Ct);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
        created.ActionName.Should().Be(nameof(_sut.GetProductById));
        created.RouteValues!["id"].Should().Be(5);
        created.Value.Should().BeEquivalentTo(response);
    }

    // ════════════════════════════════════════════════════════════════
    // PUT api/products/{id}
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Update_Returns200_WhenSuccessful()
    {
        var request = ProductFixtures.BuildRequest(name: "Updated");
        var response = ProductFixtures.BuildResponse(id: 1, name: "Updated");
        _service.Setup(s => s.UpdateProductAsync(1, request, Ct)).ReturnsAsync(response);

        var result = await _sut.UpdateProduct(1, request, Ct);

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Update_Propagates_KeyNotFoundException()
    {
        _service.Setup(s => s.UpdateProductAsync(99, It.IsAny<ProductRequest>(), Ct))
            .ThrowsAsync(new KeyNotFoundException("Product with id 99 was not found."));

        var act = async () => await _sut.UpdateProduct(99, ProductFixtures.BuildRequest(), Ct);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*99*");
    }

    // ════════════════════════════════════════════════════════════════
    // PATCH api/products/{id}/stock
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateStock_Returns404_WhenProductNotFound()
    {
        _service.Setup(s => s.GetProductByIdAsync(99, Ct))
            .ReturnsAsync((ProductResponse?)null);

        var result = await _sut.UpdateStockCount(99, 10, Ct);

        result.Should().BeOfType<NotFoundResult>();
        _service.Verify(s =>
            s.UpdateProductAsync(It.IsAny<int>(), It.IsAny<ProductRequest>(), Ct), Times.Never);
    }

    [Fact]
    public async Task UpdateStock_CallsUpdateProduct_WithNewStockCount()
    {
        var existing = ProductFixtures.BuildResponse(id: 1, stock: 5);
        var updated = ProductFixtures.BuildResponse(id: 1, stock: 10);

        _service.Setup(s => s.GetProductByIdAsync(1, Ct)).ReturnsAsync(existing);
        _service.Setup(s => s.UpdateProductAsync(1, It.IsAny<ProductRequest>(), Ct))
            .ReturnsAsync(updated);

        await _sut.UpdateStockCount(1, 10, Ct);

        _service.Verify(s => s.UpdateProductAsync(
            1,
            It.Is<ProductRequest>(r =>
                r.StockCount == 10 &&
                r.Name == existing.Name &&
                r.TickerSymbol == existing.TickerSymbol &&
                r.OriginPrice == existing.OriginPrice &&
                r.CurrentPrice == existing.CurrentPrice &&
                r.PriceAlertThreshold == existing.PriceAlertThreshold),
            Ct), Times.Once);
    }

    [Fact]
    public async Task UpdateStock_Returns200_WhenSuccessful()
    {
        var existing = ProductFixtures.BuildResponse(id: 1);
        var updated = ProductFixtures.BuildResponse(id: 1, stock: 10);

        _service.Setup(s => s.GetProductByIdAsync(1, Ct)).ReturnsAsync(existing);
        _service.Setup(s => s.UpdateProductAsync(1, It.IsAny<ProductRequest>(), Ct))
            .ReturnsAsync(updated);

        var result = await _sut.UpdateStockCount(1, 10, Ct);

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    // ════════════════════════════════════════════════════════════════
    // DELETE api/products/{id}
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Delete_Returns204_WhenDeleted()
    {
        _service.Setup(s => s.DeleteProductAsync(1, Ct))
            .ReturnsAsync(ProductFixtures.BuildResponse(id: 1));

        var result = await _sut.DeleteProduct(1, Ct);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_Returns404_WhenNotFound()
    {
        _service.Setup(s => s.DeleteProductAsync(99, Ct))
            .ReturnsAsync((ProductResponse?)null);

        var result = await _sut.DeleteProduct(99, Ct);

        result.Should().BeOfType<NotFoundResult>();
    }

    // ════════════════════════════════════════════════════════════════
    // POST api/products/bulk
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task BulkInsert_Returns204_Always()
    {
        var requests = new[] { ProductFixtures.BuildRequest(), ProductFixtures.BuildRequest() };
        _service.Setup(s => s.BulkInsertProductsAsync(requests, Ct)).Returns(Task.CompletedTask);

        var result = await _sut.BulkInsertProducts(requests, Ct);

        result.Should().BeOfType<NoContentResult>();
    }

    // ════════════════════════════════════════════════════════════════
    // GET api/products/price-alerts
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PriceAlerts_Returns200_WithAlerts()
    {
        var alerts = new[]
        {
            new PriceLossResponse { Id = 1, PriceDiff = -20m, PriceChangePercent = 0.2m },
            new PriceLossResponse { Id = 2, PriceDiff = -30m, PriceChangePercent = 0.3m }
        };
        _service.Setup(s => s.GetPriceLossAlertsAsync(Ct)).ReturnsAsync(alerts);

        var result = await _sut.GetPriceLossAlerts(Ct);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.As<IEnumerable<PriceLossResponse>>().Should().HaveCount(2);
    }
}
