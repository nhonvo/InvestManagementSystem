using InventoryAlert.Contracts.Events.Payloads;
using InventoryAlert.Contracts.Persistence;
using InventoryAlert.Contracts.Persistence.Entities;
using InventoryAlert.Contracts.Persistence.Repositories;
using InventoryAlert.UnitTests.Helpers;
using InventoryAlert.Worker.Application.IntegrationHandlers;
using InventoryAlert.Worker.Infrastructure.External.Finnhub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Worker.Handlers;

public class NewsHandlerTests : IDisposable
{
    private readonly InventoryDbContext _db;
    private readonly Mock<NewsDynamoRepository> _newsRepoMock;
    private readonly Mock<IFinnhubClient> _finnhubMock = new();
    private readonly Mock<ILogger<NewsHandler>> _loggerMock = new();
    private readonly NewsHandler _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public NewsHandlerTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new InventoryDbContext(options);

        var dynamoMock = new Mock<Amazon.DynamoDBv2.IAmazonDynamoDB>();
        var repoLoggerMock = new Mock<ILogger<NewsDynamoRepository>>();
        _newsRepoMock = new Mock<NewsDynamoRepository>(dynamoMock.Object, repoLoggerMock.Object);

        _sut = new NewsHandler(_db, _newsRepoMock.Object, _finnhubMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task HandleAsync_FetchesFromFinnhub_AndSavesNews_WhenProductExists()
    {
        // Arrange
        var ticker = "AAPL";
        var product = ProductFixtures.BuildProduct(ticker: ticker);
        _db.Products.Add(product);
        await _db.SaveChangesAsync(Ct);

        var payload = new CompanyNewsAlertPayload
        {
            Symbol = ticker
        };

        var articles = new List<NewsArticle>
        {
            new NewsArticle { Id = 12345, Headline = "Apple is doing great", Datetime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
        };

        _finnhubMock.Setup(f => f.FetchNewsAsync(ticker, It.IsAny<string>(), It.IsAny<string>(), Ct))
            .ReturnsAsync(articles);

        // Act
        await _sut.HandleAsync(payload, Ct);

        // Assert
        _newsRepoMock.Verify(r => r.SaveAsync(It.Is<NewsDynamoEntry>(e =>
            e.TickerSymbol == ticker &&
            e.Headline == "Apple is doing great" &&
            e.FinnhubId == 12345), Ct), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Skips_WhenProductDoesNotExist()
    {
        // Arrange
        var payload = new CompanyNewsAlertPayload { Symbol = "NOTFOUND" };

        // Act
        await _sut.HandleAsync(payload, Ct);

        // Assert
        _newsRepoMock.Verify(r => r.SaveAsync(It.IsAny<NewsDynamoEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
