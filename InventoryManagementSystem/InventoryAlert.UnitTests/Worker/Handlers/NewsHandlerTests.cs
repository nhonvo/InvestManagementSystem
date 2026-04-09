using InventoryAlert.Contracts.Events.Payloads;
using InventoryAlert.Contracts.Persistence.Entities;
using InventoryAlert.Contracts.Persistence.Interfaces;
using InventoryAlert.UnitTests.Helpers;
using InventoryAlert.Worker.Application.IntegrationHandlers;
using InventoryAlert.Worker.Infrastructure.External.Finnhub;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Worker.Handlers;

public class NewsHandlerTests : IDisposable
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<INewsDynamoRepository> _newsRepoMock = new();
    private readonly Mock<IFinnhubClient> _finnhubMock = new();
    private readonly Mock<ILogger<NewsHandler>> _loggerMock = new();
    private readonly NewsHandler _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public NewsHandlerTests()
    {
        _sut = new NewsHandler(_productRepoMock.Object, _newsRepoMock.Object, _finnhubMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task HandleAsync_FetchesFromFinnhub_AndSavesNews_WhenProductExists()
    {
        // Arrange
        var ticker = "AAPL";
        var product = ProductFixtures.BuildProduct(ticker: ticker);
        _productRepoMock.Setup(r => r.GetByTickerAsync(ticker, Ct)).ReturnsAsync(product);

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
        _newsRepoMock.Verify(r => r.BatchSaveAsync(It.Is<IEnumerable<NewsDynamoEntry>>(e => e.Any(x =>
            x.TickerSymbol == ticker &&
            x.Headline == "Apple is doing great" &&
            x.FinnhubId == 12345)), Ct), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Skips_WhenProductDoesNotExist()
    {
        // Arrange
        var payload = new CompanyNewsAlertPayload { Symbol = "NOTFOUND" };

        // Act
        await _sut.HandleAsync(payload, Ct);

        // Assert
        _newsRepoMock.Verify(r => r.BatchSaveAsync(It.IsAny<IEnumerable<NewsDynamoEntry>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
