using InventoryAlert.Contracts.Events.Payloads;
using InventoryAlert.Contracts.Persistence;
using InventoryAlert.Contracts.Persistence.Entities;
using InventoryAlert.Contracts.Persistence.Interfaces;
using InventoryAlert.UnitTests.Helpers;
using InventoryAlert.Worker.Application.Interfaces.Handlers;
using InventoryAlert.Worker.Infrastructure.External.Finnhub;
using InventoryAlert.Worker.Infrastructure.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Worker.Jobs;

public class NewsCheckJobTests : IDisposable
{
    private readonly InventoryDbContext _db;
    private readonly Mock<IDistributedCache> _cacheMock = new();
    private readonly Mock<IFinnhubClient> _finnhubMock = new();
    private readonly Mock<INewsDynamoRepository> _newsRepoMock = new();
    private readonly Mock<ILogger<NewsCheckJob>> _loggerMock = new();
    private readonly NewsCheckJob _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public NewsCheckJobTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new InventoryDbContext(options);

        _sut = new NewsCheckJob(_db, _cacheMock.Object, _finnhubMock.Object, _newsRepoMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ExecuteAsync_SavesNewsToRepo_WhenNewNewsFound()
    {
        // Arrange
        var ticker = "AAPL";
        _db.Products.Add(ProductFixtures.BuildProduct(ticker: ticker));
        await _db.SaveChangesAsync(Ct);

        var news = new List<NewsArticle>
        {
            new NewsArticle { Headline = "New Apple News", Summary = "...", Datetime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Id = 1 }
        };

        _finnhubMock.Setup(f => f.FetchNewsAsync(ticker, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(news);

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[])null!);

        // Act
        await _sut.ExecuteAsync(Ct);

        // Assert
        _newsRepoMock.Verify(h => h.BatchSaveAsync(It.Is<IEnumerable<NewsDynamoEntry>>(e => e.Any(x => x.TickerSymbol == ticker)), It.IsAny<CancellationToken>()), Times.Once);

        _cacheMock.Verify(c => c.SetAsync(It.Is<string>(k => k.Contains(ticker)), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
