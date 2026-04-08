using FluentAssertions;
using InventoryAlert.Api.Application.Common.Exceptions;
using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Web.Controllers;
using InventoryAlert.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Web.Controllers;

public class ProductsControllerTests
{
    private readonly Mock<IProductService> _service = new();
    private readonly ProductsController _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public ProductsControllerTests()
    {
        _sut = new ProductsController(_service.Object);
        // Mock UrlHelper for HATEOAS links
        var urlHelper = new Mock<IUrlHelper>();
        _sut.Url = urlHelper.Object;
    }

    // ════════════════════════════════════════════════════════════════
    // GET api/products
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetProducts_Returns200_WithList()
    {
        var responses = new PagedResult<ProductResponse>
        {
            Items =
            [
                ProductFixtures.BuildResponse(id: 1),
                ProductFixtures.BuildResponse(id: 2)
            ],
            TotalItems = 2,
            PageNumber = 1,
            PageSize = 10
        };
        _service.Setup(s => s.GetProductsPagedAsync(It.IsAny<ProductQueryParams>(), Ct)).ReturnsAsync(responses);

        var result = await _sut.GetProducts(new ProductQueryParams(), Ct);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.As<PagedResult<ProductResponse>>().Items.Should().HaveCount(2);
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
            .ThrowsAsync(new UserFriendlyException(ErrorCode.NotFound, "Product not found"));

        var act = () => _sut.GetProductById(99, Ct);

        await act.Should().ThrowAsync<UserFriendlyException>()
            .Where(e => e.ErrorCode == ErrorCode.NotFound);
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
    public async Task Update_Propagates_UserFriendlyException()
    {
        _service.Setup(s => s.UpdateProductAsync(99, It.IsAny<ProductRequest>(), Ct))
            .ThrowsAsync(new UserFriendlyException(ErrorCode.NotFound, "Product with id 99 was not found."));

        var act = async () => await _sut.UpdateProduct(99, ProductFixtures.BuildRequest(), Ct);

        await act.Should().ThrowAsync<UserFriendlyException>()
            .Where(e => e.ErrorCode == ErrorCode.NotFound && e.Message.Contains("99"));
    }

    // ════════════════════════════════════════════════════════════════
    // DELETE api/products/{id}
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Delete_Returns200_WhenDeleted()
    {
        _service.Setup(s => s.DeleteProductAsync(1, Ct))
            .ReturnsAsync(ProductFixtures.BuildResponse(id: 1));

        var result = await _sut.DeleteProduct(1, Ct);

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Delete_Returns404_WhenNotFound()
    {
        _service.Setup(s => s.DeleteProductAsync(99, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UserFriendlyException(ErrorCode.NotFound, "Product not found"));

        var act = () => _sut.DeleteProduct(99, Ct);

        await act.Should().ThrowAsync<UserFriendlyException>()
            .Where(e => e.ErrorCode == ErrorCode.NotFound);
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
