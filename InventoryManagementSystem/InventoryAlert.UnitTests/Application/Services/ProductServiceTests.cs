using FluentAssertions;
using FluentValidation;
using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Application.Services;
using InventoryAlert.Api.Web.Configuration;
using InventoryAlert.Contracts.Common.Exceptions;
using InventoryAlert.Contracts.Configuration;
using InventoryAlert.Contracts.Persistence.Interfaces;
using InventoryAlert.UnitTests.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Application.Services;

public class ProductServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IFinnhubClient> _finnhubClient = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly AppSettings _appSettings;
    private readonly Mock<IValidator<ProductRequest>> _validator = new();
    private readonly Mock<IStockTransactionRepository> _stockTxRepo = new();
    private readonly Mock<ILogger<ProductService>> _logger = new();
    private readonly ProductService _sut;

    private static readonly CancellationToken Ct = CancellationToken.None;

    public ProductServiceTests()
    {
        _appSettings = new AppSettings { Cache = new CacheSettings { ProductTtlMinutes = 10 } };
        SetupTransactionExecution();

        _validator.Setup(v => v.ValidateAsync(It.IsAny<ProductRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _sut = new ProductService(
            _unitOfWork.Object,
            _productRepository.Object,
            _finnhubClient.Object,
            _cache,
            _appSettings,
            _validator.Object,
            _stockTxRepo.Object,
            _logger.Object);
    }


    // ────────────────────────────────────────────────────────────────
    // Helper: set up ExecuteTransactionAsync to actually invoke delegate
    // ────────────────────────────────────────────────────────────────
    private void SetupTransactionExecution()
    {
        _unitOfWork
            .Setup(u => u.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((action, _) => action());
    }

    // ════════════════════════════════════════════════════════════════
    // GetAllProductsAsync
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoProductsExist()
    {
        _productRepository
            .Setup(r => r.GetAllAsync(Ct))
            .ReturnsAsync([]);

        var result = await _sut.GetAllProductsAsync(Ct);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_ReturnsMappedResponses_WhenProductsExist()
    {
        var products = new List<Product>
        {
            ProductFixtures.BuildProduct(id: 1, name: "Apple", ticker: "AAPL", originPrice: 250m, currentPrice: 200m, threshold: 0.2, stock: 50),
            ProductFixtures.BuildProduct(id: 2, name: "Google", ticker: "GOOGL", originPrice: 300m, currentPrice: 290m, threshold: 0.1, stock: 100)
        };
        _productRepository.Setup(r => r.GetAllAsync(Ct)).ReturnsAsync(products);

        var result = (await _sut.GetAllProductsAsync(Ct)).ToList();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1);
        result[0].Name.Should().Be("Apple");
        result[0].TickerSymbol.Should().Be("AAPL");
        result[0].OriginPrice.Should().Be(250m);
        result[0].CurrentPrice.Should().Be(200m);
        result[0].PriceAlertThreshold.Should().Be(0.2);
        result[0].StockCount.Should().Be(50);
    }

    // ════════════════════════════════════════════════════════════════
    // GetProductByIdAsync
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetById_ThrowsUserFriendlyException_WhenProductNotFound()
    {
        _productRepository.Setup(r => r.GetByIdAsync(99, Ct)).ReturnsAsync((Product?)null);

        var act = () => _sut.GetProductByIdAsync(99, Ct);

        await act.Should().ThrowAsync<UserFriendlyException>()
            .Where(e => e.ErrorCode == ErrorCode.NotFound);
    }

    [Fact]
    public async Task GetById_ReturnsMappedResponse_WhenProductFound()
    {
        var product = ProductFixtures.BuildProduct(id: 1);
        _productRepository.Setup(r => r.GetByIdAsync(1, Ct)).ReturnsAsync(product);

        var result = await _sut.GetProductByIdAsync(1, Ct);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be(product.Name);
        result.TickerSymbol.Should().Be(product.TickerSymbol);
    }

    [Fact]
    public async Task Create_CallsAddAsync_AndSaveChanges()
    {
        var request = ProductFixtures.BuildRequest();
        var entity = ProductFixtures.BuildProduct();
        _productRepository.Setup(r => r.AddAsync(It.IsAny<Product>(), Ct)).ReturnsAsync(entity);

        await _sut.CreateProductAsync(request, Ct);

        _productRepository.Verify(r => r.AddAsync(It.IsAny<Product>(), Ct), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }

    [Fact]
    public async Task Create_ReturnsMappedResponse_WithCorrectFields()
    {
        var request = ProductFixtures.BuildRequest(name: "Apple", ticker: "AAPL", price: 250m, threshold: 0.2, stock: 50);
        var entity = ProductFixtures.BuildProduct(id: 5, name: "Apple", ticker: "AAPL", originPrice: 250m, currentPrice: 200m, threshold: 0.2, stock: 50);
        _productRepository.Setup(r => r.AddAsync(It.IsAny<Product>(), Ct)).ReturnsAsync(entity);

        var result = await _sut.CreateProductAsync(request, Ct);

        result.Id.Should().Be(5);
        result.Name.Should().Be("Apple");
        result.TickerSymbol.Should().Be("AAPL");
        result.OriginPrice.Should().Be(250m);
        result.CurrentPrice.Should().Be(200m);
        result.PriceAlertThreshold.Should().Be(0.2);
        result.StockCount.Should().Be(50);
    }

    [Fact]
    public async Task Update_ThrowsConflict_WhenTickerChangedToExistingAnother()
    {
        var existing = ProductFixtures.BuildProduct(id: 1, ticker: "AAPL");
        var request = ProductFixtures.BuildRequest(ticker: "MSFT");

        _productRepository.Setup(r => r.GetByIdAsync(1, Ct)).ReturnsAsync(existing);
        _productRepository.Setup(r => r.GetByTickerAsync("MSFT", Ct))
            .ReturnsAsync(ProductFixtures.BuildProduct(id: 2, ticker: "MSFT"));

        var act = () => _sut.UpdateProductAsync(1, request, Ct);

        await act.Should().ThrowAsync<UserFriendlyException>()
            .Where(e => e.ErrorCode == ErrorCode.Conflict);
    }

    [Fact]
    public async Task Update_DoesNotThrow_WhenTickerSameAsSelf()
    {
        var existing = ProductFixtures.BuildProduct(id: 1, ticker: "AAPL");
        var request = ProductFixtures.BuildRequest(ticker: "AAPL", name: "Updated Name");

        _productRepository.Setup(u => u.GetByIdAsync(1, Ct)).ReturnsAsync(existing);
        _productRepository.Setup(r => r.UpdateAsync(It.IsAny<Product>())).ReturnsAsync(existing);

        var result = await _sut.UpdateProductAsync(1, request, Ct);

        result.Should().NotBeNull();
        _productRepository.Verify(r => r.GetByTickerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_MapsNullTickerSymbol_ToEmptyString()
    {
        var request = ProductFixtures.BuildRequest(ticker: null);
        Product? capturedEntity = null;
        _productRepository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), Ct))
            .Callback<Product, CancellationToken>((p, _) => capturedEntity = p)
            .ReturnsAsync(ProductFixtures.BuildProduct());

        await _sut.CreateProductAsync(request, Ct);

        capturedEntity!.TickerSymbol.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_ThrowsConflict_WhenTickerAlreadyExists()
    {
        var request = ProductFixtures.BuildRequest(ticker: "AAPL");
        _productRepository.Setup(r => r.GetByTickerAsync("AAPL", Ct))
            .ReturnsAsync(ProductFixtures.BuildProduct(id: 1, ticker: "AAPL"));

        var act = () => _sut.CreateProductAsync(request, Ct);

        await act.Should().ThrowAsync<UserFriendlyException>()
            .Where(e => e.ErrorCode == ErrorCode.Conflict);
    }

    [Fact]
    public async Task Update_ThrowsUserFriendlyException_WhenProductNotFound()
    {
        _productRepository.Setup(r => r.GetByIdAsync(99, Ct)).ReturnsAsync((Product?)null);

        var act = async () => await _sut.UpdateProductAsync(99, ProductFixtures.BuildRequest(), Ct);

        await act.Should().ThrowAsync<UserFriendlyException>()
            .Where(e => e.ErrorCode == ErrorCode.NotFound && e.Message.Contains("99"));
    }

    [Fact]
    public async Task Update_UpdatesAllFields_WhenProductFound()
    {
        var existing = ProductFixtures.BuildProduct(id: 1);
        var request = ProductFixtures.BuildRequest(name: "Updated", ticker: "NEW", price: 999m, threshold: 0.5, stock: 77);

        _productRepository.Setup(r => r.GetByIdAsync(1, Ct)).ReturnsAsync(existing);
        SetupTransactionExecution();

        Product? capturedEntity = null;
        _productRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Product>()))
            .Callback<Product>(p => capturedEntity = p)
            .ReturnsAsync(existing);

        await _sut.UpdateProductAsync(1, request, Ct);

        capturedEntity!.Name.Should().Be("Updated");
        capturedEntity.TickerSymbol.Should().Be("NEW");
        capturedEntity.OriginPrice.Should().Be(999m);
        // CurrentPrice is not updated from request anymore
        capturedEntity.CurrentPrice.Should().Be(existing.CurrentPrice);
        capturedEntity.PriceAlertThreshold.Should().Be(0.5);
        capturedEntity.StockCount.Should().Be(77);
    }

    [Fact]
    public async Task Update_ExecutesInsideTransaction()
    {
        var existing = ProductFixtures.BuildProduct(id: 1);
        _productRepository.Setup(r => r.GetByIdAsync(1, Ct)).ReturnsAsync(existing);
        _productRepository.Setup(r => r.UpdateAsync(It.IsAny<Product>())).ReturnsAsync(existing);

        await _sut.UpdateProductAsync(1, ProductFixtures.BuildRequest(), Ct);

        _unitOfWork.Verify(u =>
            u.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), Ct), Times.Once);
    }

    [Fact]
    public async Task Update_ReturnsUpdatedProduct_NotBlankDefault()
    {
        // Bug #3 regression: updated = new() before lambda — response must reflect the real entity
        var existing = ProductFixtures.BuildProduct(id: 1, name: "Before");
        var updated = ProductFixtures.BuildProduct(id: 1, name: "After");

        _productRepository.Setup(r => r.GetByIdAsync(1, Ct)).ReturnsAsync(existing);
        _productRepository.Setup(r => r.UpdateAsync(It.IsAny<Product>())).ReturnsAsync(updated);

        var result = await _sut.UpdateProductAsync(1, ProductFixtures.BuildRequest(name: "After"), Ct);

        result.Id.Should().Be(1, "must not return id=0 from blank new Product()");
        result.Name.Should().Be("After");
    }

    [Fact]
    public async Task UpdateStockCount_UpdatesOnlyStockCount_LogsTransaction_AndClearsCache()
    {
        var existing = ProductFixtures.BuildProduct(id: 1, stock: 10);
        _productRepository.Setup(r => r.GetByIdAsync(1, Ct)).ReturnsAsync(existing);
        _productRepository.Setup(r => r.UpdateAsync(It.IsAny<Product>())).Returns(Task.FromResult(existing));

        var result = await _sut.UpdateStockCountAsync(1, 99, "test-user", Ct);

        result.StockCount.Should().Be(99);
        _productRepository.Verify(r => r.UpdateAsync(It.Is<Product>(p => p.StockCount == 99)), Times.Once);
        _stockTxRepo.Verify(r => r.AddAsync(It.Is<InventoryAlert.Contracts.Entities.StockTransaction>(t =>
            t.ProductId == 1 && t.Quantity == 89 && t.UserId == "test-user"), Ct), Times.Once);
        _unitOfWork.Verify(u => u.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), Ct), Times.Once);
    }

    // ════════════════════════════════════════════════════════════════
    // DeleteProductAsync
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Delete_ThrowsUserFriendlyException_WhenProductNotFound()
    {
        _productRepository.Setup(r => r.GetByIdAsync(99, Ct)).ReturnsAsync((Product?)null);

        var act = () => _sut.DeleteProductAsync(99, Ct);

        await act.Should().ThrowAsync<UserFriendlyException>()
            .Where(e => e.ErrorCode == ErrorCode.NotFound);

        _unitOfWork.Verify(u =>
            u.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), Ct), Times.Never);
    }

    [Fact]
    public async Task Delete_CallsDeleteAsync_InsideTransaction_WhenFound()
    {
        var product = ProductFixtures.BuildProduct(id: 1);
        _productRepository.Setup(r => r.GetByIdAsync(1, Ct)).ReturnsAsync(product);
        _productRepository.Setup(r => r.DeleteAsync(product)).ReturnsAsync(product);

        await _sut.DeleteProductAsync(1, Ct);

        _productRepository.Verify(r => r.DeleteAsync(product), Times.Once);
        _unitOfWork.Verify(u =>
            u.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), Ct), Times.Once);
    }

    [Fact]
    public async Task Delete_ReturnsMappedResponse_WhenFound()
    {
        var product = ProductFixtures.BuildProduct(id: 3, name: "Microsoft", ticker: "MSFT");
        _productRepository.Setup(r => r.GetByIdAsync(3, Ct)).ReturnsAsync(product);
        _productRepository.Setup(r => r.DeleteAsync(product)).ReturnsAsync(product);

        var result = await _sut.DeleteProductAsync(3, Ct);

        result.Should().NotBeNull();
        result!.Id.Should().Be(3);
        result.Name.Should().Be("Microsoft");
        result.TickerSymbol.Should().Be("MSFT");
    }

    // ════════════════════════════════════════════════════════════════
    // BulkInsertProductsAsync
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task BulkInsert_CallsAddRangeAsync_InsideTransaction()
    {
        var requests = new[]
        {
            ProductFixtures.BuildRequest(name: "A", ticker: "A"),
            ProductFixtures.BuildRequest(name: "B", ticker: "B"),
            ProductFixtures.BuildRequest(name: "C", ticker: "C")
        };

        _productRepository.Setup(r => r.GetExistingTickersAsync(It.IsAny<IEnumerable<string>>(), Ct))
            .ReturnsAsync(Enumerable.Empty<string>());

        await _sut.BulkInsertProductsAsync(requests, Ct);

        _productRepository.Verify(r =>
            r.AddRangeAsync(It.Is<IEnumerable<Product>>(l => l.Count() == 3), Ct), Times.Once);
        _unitOfWork.Verify(u =>
            u.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), Ct), Times.Once);
    }

    [Fact]
    public async Task BulkInsert_MapsAllRequests_ToEntities()
    {
        var requests = new[]
        {
            ProductFixtures.BuildRequest(name: "Alpha", ticker: "ALP", price: 10m),
            ProductFixtures.BuildRequest(name: "Beta",  ticker: "BET", price: 20m),
        };

        _productRepository.Setup(r => r.GetExistingTickersAsync(It.IsAny<IEnumerable<string>>(), Ct))
            .ReturnsAsync(Enumerable.Empty<string>());

        IEnumerable<Product>? capturedEntities = null;
        _productRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Product>>(), Ct))
            .Callback<IEnumerable<Product>, CancellationToken>((l, _) => capturedEntities = l)
            .Returns(Task.CompletedTask);

        await _sut.BulkInsertProductsAsync(requests, Ct);

        capturedEntities.Should().NotBeNull();
        capturedEntities!.Should().HaveCount(2);
        var list = capturedEntities!.ToList();
        list[0].Name.Should().Be("Alpha");
        list[0].TickerSymbol.Should().Be("ALP");
        list[0].OriginPrice.Should().Be(10m);
        list[1].Name.Should().Be("Beta");
        list[1].TickerSymbol.Should().Be("BET");
        list[1].OriginPrice.Should().Be(20m);
    }

    [Fact]
    public async Task BulkInsert_ThrowsException_WhenTickerAlreadyExists()
    {
        var requests = new[] { ProductFixtures.BuildRequest(ticker: "DUP") };
        _productRepository.Setup(r => r.GetExistingTickersAsync(It.IsAny<IEnumerable<string>>(), Ct))
            .ReturnsAsync(["DUP"]);

        var act = () => _sut.BulkInsertProductsAsync(requests, Ct);

        await act.Should().ThrowAsync<UserFriendlyException>()
            .Where(e => e.ErrorCode == ErrorCode.Conflict);
    }

    // ════════════════════════════════════════════════════════════════
    // GetPriceLossAlertsAsync
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PriceLoss_ReturnsEmpty_WhenNoProducts()
    {
        _productRepository.Setup(r => r.GetAllAsync(Ct)).ReturnsAsync([]);

        var result = await _sut.GetPriceLossAlertsAsync(Ct);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task PriceLoss_SkipsProduct_WhenCurrentPriceIsZero()
    {
        // CurrentPrice=0 → skip (would produce false loss alerts)
        var product = ProductFixtures.BuildProduct(ticker: "TST", originPrice: 100m, currentPrice: 0m, threshold: 0.1);
        _productRepository.Setup(r => r.GetAllAsync(Ct)).ReturnsAsync([product]);

        var result = await _sut.GetPriceLossAlertsAsync(Ct);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task PriceLoss_SkipsProduct_WhenOriginPriceIsZero()
    {
        var product = ProductFixtures.BuildProduct(ticker: "TST", originPrice: 0m, currentPrice: 90m, threshold: 0.1);
        _productRepository.Setup(r => r.GetAllAsync(Ct)).ReturnsAsync([product]);

        var result = await _sut.GetPriceLossAlertsAsync(Ct);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task PriceLoss_ReturnsAlert_WhenDropExceedsThreshold()
    {
        // 15% drop > 10% threshold → alert fires
        var product = ProductFixtures.BuildProduct(id: 1, ticker: "TST", originPrice: 100m, currentPrice: 85m, threshold: 0.1, stock: 5);
        _productRepository.Setup(r => r.GetAllAsync(Ct)).ReturnsAsync([product]);

        var result = (await _sut.GetPriceLossAlertsAsync(Ct)).ToList();

        result.Should().HaveCount(1);
        // priceDiff = 85 - 100 = -15 → abs = 15 → stored as positive PriceDiff
        result[0].PriceDiff.Should().Be(15m);
        // PriceChangePercent = -15/100 = -0.15  (negative, rounded)
        result[0].PriceChangePercent.Should().Be(-0.15m);
    }

    [Fact]
    public async Task PriceLoss_DoesNotAlert_WhenDropBelowThreshold()
    {
        // default currentPrice=90, originPrice=100 → 10% drop, threshold=0.2 → no alert
        var product = ProductFixtures.BuildProduct(id: 1, ticker: "TST", originPrice: 100m, currentPrice: 90m, threshold: 0.2);
        _productRepository.Setup(r => r.GetAllAsync(Ct)).ReturnsAsync([product]);

        var result = await _sut.GetPriceLossAlertsAsync(Ct);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task PriceLoss_AlertMapsAllFields_Correctly()
    {
        var product = ProductFixtures.BuildProduct(
            id: 7, name: "Apple", ticker: "AAPL",
            originPrice: 100m, currentPrice: 80m, threshold: 0.1, stock: 5);

        _productRepository.Setup(r => r.GetAllAsync(Ct)).ReturnsAsync([product]);

        var result = (await _sut.GetPriceLossAlertsAsync(Ct)).Single();

        result.Id.Should().Be(7);
        result.Name.Should().Be("Apple");
        result.TickerSymbol.Should().Be("AAPL");
        result.OriginPrice.Should().Be(100m);
        result.CurrentPrice.Should().Be(80m);
        result.PriceDiff.Should().Be(20m);              // abs(80 - 100) = 20
        result.PriceChangePercent.Should().Be(-0.2m);   // (80-100)/100
        result.StockCount.Should().Be(5);
    }

    // ════════════════════════════════════════════════════════════════
    // SyncCurrentPricesAsync
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Sync_SkipsProduct_WhenFinnhubReturnsNull()
    {
        var product = ProductFixtures.BuildProduct(id: 1, ticker: "TST", currentPrice: 50m);
        _productRepository.Setup(r => r.GetAllAsync(Ct)).ReturnsAsync([product]);
        _finnhubClient.Setup(f => f.GetQuoteAsync("TST", Ct))
            .ReturnsAsync((FinnhubQuoteResponse?)null);

        await _sut.SyncCurrentPricesAsync(Ct);

        product.CurrentPrice.Should().Be(50m, "price must not change when Finnhub returns null");
    }

    [Fact]
    public async Task Sync_UpdatesCurrentPrice_WhenValidQuote()
    {
        var product = ProductFixtures.BuildProduct(id: 1, ticker: "TST", currentPrice: 50m);
        _productRepository.Setup(r => r.GetAllAsync(Ct)).ReturnsAsync([product]);
        _finnhubClient.Setup(f => f.GetQuoteAsync("TST", Ct))
            .ReturnsAsync(ProductFixtures.BuildQuote(currentPrice: 75m));

        await _sut.SyncCurrentPricesAsync(Ct);

        product.CurrentPrice.Should().Be(75m);
    }

    [Fact]
    public async Task Sync_CallsUpdateRangeAsync_InsideTransaction()
    {
        var product = ProductFixtures.BuildProduct(id: 1, ticker: "TST");
        _productRepository.Setup(r => r.GetAllAsync(Ct)).ReturnsAsync([product]);
        _finnhubClient.Setup(f => f.GetQuoteAsync("TST", Ct))
            .ReturnsAsync(ProductFixtures.BuildQuote(currentPrice: 75m));

        await _sut.SyncCurrentPricesAsync(Ct);

        _productRepository.Verify(r =>
            r.UpdateRangeAsync(It.IsAny<IEnumerable<Product>>()), Times.Once);
        _unitOfWork.Verify(u =>
            u.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), Ct), Times.Once);
    }
}
