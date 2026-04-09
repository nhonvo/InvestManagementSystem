using FluentAssertions;
using InventoryAlert.Contracts.Persistence;
using InventoryAlert.UnitTests.Helpers;
using InventoryAlert.Worker.Infrastructure.External.Finnhub;
using InventoryAlert.Worker.Infrastructure.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Worker.Jobs;

public class SyncPricesJobTests : IDisposable
{
    private readonly InventoryDbContext _db;
    private readonly Mock<IDistributedCache> _cacheMock = new();
    private readonly Mock<IFinnhubClient> _finnhubMock = new();
    private readonly Mock<ILogger<SyncPricesJob>> _loggerMock = new();
    private readonly SyncPricesJob _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public SyncPricesJobTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new InventoryDbContext(options);
        _sut = new SyncPricesJob(_db, _cacheMock.Object, _finnhubMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesPrices_WhenNotCached()
    {
        // Arrange
        var product = ProductFixtures.BuildProduct(ticker: "AAPL", currentPrice: 100m);
        _db.Products.Add(product);
        await _db.SaveChangesAsync(Ct);
        _db.Entry(product).State = EntityState.Detached; // Simulate clean start

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[])null!);
        _finnhubMock.Setup(f => f.FetchQuoteAsync("AAPL", It.IsAny<CancellationToken>())).ReturnsAsync(new FinnhubQuoteModel { CurrentPrice = 110m });

        // Act
        await _sut.ExecuteAsync(Ct);

        // Assert
        var inDb = await _db.Products.AsNoTracking().FirstAsync();
        inDb.CurrentPrice.Should().Be(110m);
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesPrices_FromCache_WhenAvailable()
    {
        // Arrange
        var product = ProductFixtures.BuildProduct(ticker: "MSFT", currentPrice: 300m);
        _db.Products.Add(product);
        await _db.SaveChangesAsync(Ct);

        _cacheMock.Setup(c => c.GetAsync("product:quote:MSFT", Ct)).ReturnsAsync(System.Text.Encoding.UTF8.GetBytes("310.00"));

        // Act
        await _sut.ExecuteAsync(Ct);

        // Assert
        product.CurrentPrice.Should().Be(310m);
        _finnhubMock.Verify(f => f.FetchQuoteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
