using InventoryAlert.Domain.Entities.Dynamodb;
using InventoryAlert.Domain.External.Finnhub;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Worker.IntegrationEvents.Handlers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Worker.Handlers;

public class SyncMarketNewsHandlerTests
{
    private readonly Mock<IFinnhubClient> _finnhubMock = new();
    private readonly Mock<IMarketNewsDynamoRepository> _newsRepoMock = new();
    private readonly Mock<ILogger<SyncMarketNewsHandler>> _loggerMock = new();
    private readonly SyncMarketNewsHandler _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public SyncMarketNewsHandlerTests()
    {
        _sut = new SyncMarketNewsHandler(_finnhubMock.Object, _newsRepoMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_SyncsAllCategories_AndSavesToDynamo()
    {
        // Arrange
        var generalNews = new List<FinnhubNewsItem> { new() { Id = 1, Headline = "General", Datetime = 1712217600 } };
        var forexNews = new List<FinnhubNewsItem> { new() { Id = 2, Headline = "Forex", Datetime = 1712217601 } };

        _finnhubMock.Setup(f => f.GetMarketNewsAsync("general", Ct)).ReturnsAsync(generalNews);
        _finnhubMock.Setup(f => f.GetMarketNewsAsync("forex", Ct)).ReturnsAsync(forexNews);
        _finnhubMock.Setup(f => f.GetMarketNewsAsync("crypto", Ct)).ReturnsAsync(new List<FinnhubNewsItem>());
        _finnhubMock.Setup(f => f.GetMarketNewsAsync("merger", Ct)).ReturnsAsync(new List<FinnhubNewsItem>());

        // Act
        await _sut.HandleAsync(Ct);

        // Assert
        _finnhubMock.Verify(f => f.GetMarketNewsAsync(It.IsAny<string>(), Ct), Times.Exactly(4));
        _newsRepoMock.Verify(r => r.BatchSaveAsync(It.IsAny<IEnumerable<MarketNewsDynamoEntry>>(), Ct), Times.Exactly(2));
    }

    [Fact]
    public async Task HandleAsync_Continues_WhenCategoryReturnsNoArticles()
    {
        // Arrange
        _finnhubMock.Setup(f => f.GetMarketNewsAsync(It.IsAny<string>(), Ct))
            .ReturnsAsync(new List<FinnhubNewsItem>());

        // Act
        await _sut.HandleAsync(Ct);

        // Assert
        _newsRepoMock.Verify(r => r.BatchSaveAsync(It.IsAny<IEnumerable<MarketNewsDynamoEntry>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenApiFails_LogsAndThrows()
    {
        // Arrange
        _finnhubMock.Setup(f => f.GetMarketNewsAsync(It.IsAny<string>(), Ct))
            .ThrowsAsync(new Exception("Sync failed"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _sut.HandleAsync(Ct));
    }
}
