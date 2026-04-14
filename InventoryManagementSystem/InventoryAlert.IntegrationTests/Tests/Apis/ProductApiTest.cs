using System.Net;
using FluentAssertions;
using InventoryAlert.IntegrationTests.Clients;
using InventoryAlert.IntegrationTests.Config;
using InventoryAlert.IntegrationTests.DataSources;
using InventoryAlert.IntegrationTests.Fixtures;
using InventoryAlert.IntegrationTests.Helpers;
using InventoryAlert.IntegrationTests.Models.Request;
using InventoryAlert.IntegrationTests.Models.TestData;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace InventoryAlert.IntegrationTests.Tests.Apis;

public class ProductApiTest : IClassFixture<InjectionFixture>
{
    private readonly ProductClient _client;
    // private readonly AppSettings _settings;
    private readonly ITestOutputHelper _output;

    public ProductApiTest(InjectionFixture fixture, ITestOutputHelper output)
    {
        // _settings = fixture.ServiceProvider.GetRequiredService<AppSettings>();
        _client = fixture.ServiceProvider.GetRequiredService<ProductClient>();
        _output = output;
    }

    [Fact]
    public async Task GetProducts_ReturnsListOfProducts()
    {
        // Arrange
        var getProductsRequest = new GetProductsRequest()
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "name_asc"
        };

        // Act
        var response = await _client.GetProductsAsync(getProductsRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data.Items.Should().NotBeEmpty();
        response.Data.TotalItems.Should().Be(response.Data.Items.Count);
    }

    [Fact]
    public async Task GetProductById_ReturnsExpectedProduct_WhenProductExists()
    {
        // Arrange

        // Act
        var response = await _client.GetProductByIdAsync(1);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetProductById_ReturnsNotFound_WhenProductDoesNotExist()
    {
        // Arrange

        // Act
        var response = await _client.GetProductByIdAsync(4000);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateProduct_ReturnsCreatedProduct_WhenRequestIsValid()
    {
        // Arrange
        var createRequest = new CreateUpdateProductRequest
        {
            Name = "Test Product",
            TickerSymbol = "CREATE",
            StockCount = 100,
            Price = 10.0m,
            PriceAlertThreshold = 0.2,
            StockAlertThreshold = 20
        };
        var expectedCreateResponse = ProductHelpers.CreateExpectedProductResponse(createRequest);

        string? productId = null;

        try
        {
            // Act
            var response = await _client.CreateProductAsync(createRequest);

            _output.WriteLine("Create Product Response: {0}", response.Content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.Data.Should().NotBeNull();

            productId = response.Data.Id.ToString();

            response.Data.Should().BeEquivalentTo(expectedCreateResponse, options => options.Excluding(r => r.Id).Excluding(r => r.CurrentPrice));
        }
        finally
        {
            // Cleanup: Delete the created product if it was created successfully
            if (!string.IsNullOrEmpty(productId))
            {
                await _client.DeleteProductAsync(int.Parse(productId));
            }
        }
    }

    [Fact]
    public async Task CreateProduct_ReturnsConflict_WhenProductWithSameTickerSymbolExists()
    {
        // Arrange
        var createRequest = new CreateUpdateProductRequest
        {
            Name = "Test Product",
            TickerSymbol = "TEST",
            StockCount = 100,
            Price = 10.0m,
            PriceAlertThreshold = 0.2,
            StockAlertThreshold = 20
        };

        int? productId = null;

        try
        {
            // Act
            var firstResponse = await _client.CreateProductAsync(createRequest);
            productId = firstResponse.Data.Id;

            var secondResponse = await _client.CreateProductAsync(createRequest);

            // Assert
            secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }
        finally
        {
            // Cleanup: Delete the created product if it was created successfully
            if (productId.HasValue)
            {
                await _client.DeleteProductAsync(productId.Value);
            }
        }
    }

    [Theory]
    [MemberData(nameof(ProductData.GetCreateProductTestCases), MemberType = typeof(ProductData))]
    public async Task CreateProduct_ReturnsExpectedResponse(CreateProductTestCase testCase)
    {
        // Arrange
        _output.WriteLine($"Running test case: {testCase}");

        int productId = -1;

        try
        {
            // Act
            var response = await _client.CreateProductAsync(testCase.Request);

            // Assert
            response.Data.Should().NotBeNull();
            productId = response.Data.Id;

            response.StatusCode.Should().Be((HttpStatusCode)testCase.ExpectedStatusCode);
        }
        finally
        {
            // Cleanup: Delete the created product if it was created successfully
            if (productId != -1)
            {
                await _client.DeleteProductAsync(productId);
            }
        }
    }

    // [Fact]
    // public async Task UpdateProduct_ReturnsUpdatedProduct_WhenRequestIsValid()
    // {
    //     // Arrange
    //     var createRequest = ProductHelpers.CreateValidProductRequest();
    //     var createResponse = await _client.CreateProductAsync(createRequest);

    //     var productId = createResponse.Data.Id;

    //     var updateRequest = ProductHelpers.CreateValidUpdateProductRequest();
    //     var expectedUpdateResponse = ProductHelpers.CreateExpectedProductResponse(updateRequest, productId);

    //     try
    //     {
    //         // Act
    //         var updateResponse = await _client.UpdateProductAsync(productId, updateRequest);

    //         // Assert
    //         updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    //         updateResponse.Data.Should().NotBeNull();
    //         updateResponse.Data.Should().BeEquivalentTo(expectedUpdateResponse);
    //     }
    //     finally
    //     {
    //         // Cleanup: Delete the created product
    //         await _client.DeleteProductAsync(productId);
    //     }
    // }

    [Fact]
    public async Task DeleteProduct_ReturnsOk_WhenProductIsDeleted()
    {
        // Arrange
        var createRequest = new CreateUpdateProductRequest
        {
            Name = "Test Product",
            TickerSymbol = "DELETE",
            StockCount = 100,
            Price = 10.0m,
            PriceAlertThreshold = 0.2,
            StockAlertThreshold = 20
        };
        var createResponse = await _client.CreateProductAsync(createRequest);

        var productId = createResponse.Data.Id;

        var expectedCreateResponse = ProductHelpers.CreateExpectedProductResponse(createRequest, productId);

        // Act
        var deleteResponse = await _client.DeleteProductAsync(productId);

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        deleteResponse.Data.Should().BeEquivalentTo(expectedCreateResponse, options => options.Excluding(r => r.CurrentPrice));
    }

    [Fact]
    public async Task DeleteProduct_ReturnsNotFound_WhenProductDoesNotExist()
    {
        // Arrange

        // Act
        var deleteResponse = await _client.DeleteProductAsync(4000);

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
