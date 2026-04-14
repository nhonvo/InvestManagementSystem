using InventoryAlert.Domain.Entities.Dynamodb;
using InventoryAlert.Domain.Events.Payloads;
using InventoryAlert.Domain.External.Finnhub;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Worker.IntegrationEvents.Handlers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Worker.Handlers;

public class CompanyNewsAlertHandlerTests
{
    private readonly Mock<IFinnhubClient> _finnhubMock = new();
    private readonly Mock<ICompanyNewsDynamoRepository> _newsRepoMock = new();
    private readonly Mock<ILogger<CompanyNewsAlertHandler>> _loggerMock = new();
    private readonly CompanyNewsAlertHandler _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public CompanyNewsAlertHandlerTests()
    {
        _sut = new CompanyNewsAlertHandler(_finnhubMock.Object, _newsRepoMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenNewsFound_SavesToDynamo()
    {
        // Arrange
        var symbol = "MSFT";
        var payload = new CompanyNewsAlertPayload { Symbol = symbol };
        var articles = new List<FinnhubNewsItem>
        {
            new() { Id = 1, Headline = "Big news", Datetime = 1712217600 },
            new() { Id = 2, Headline = "Small news", Datetime = 1712217601 }
        };

        _finnhubMock.Setup(f => f.GetCompanyNewsAsync(symbol, It.IsAny<string>(), It.IsAny<string>(), Ct))
            .ReturnsAsync(articles);

        // Act
        await _sut.HandleAsync(payload, Ct);

        // Assert
        _newsRepoMock.Verify(r => r.BatchSaveAsync(It.Is<IEnumerable<CompanyNewsDynamoEntry>>(e => 
            e.Count() == 2 && e.All(x => x.Symbol == symbol)), Ct), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenNoNewsFound_DoesNotSave()
    {
        // Arrange
        var symbol = "MSFT";
        var payload = new CompanyNewsAlertPayload { Symbol = symbol };
        _finnhubMock.Setup(f => f.GetCompanyNewsAsync(symbol, It.IsAny<string>(), It.IsAny<string>(), Ct))
            .ReturnsAsync(new List<FinnhubNewsItem>());

        // Act
        await _sut.HandleAsync(payload, Ct);

        // Assert
        _newsRepoMock.Verify(r => r.BatchSaveAsync(It.IsAny<IEnumerable<CompanyNewsDynamoEntry>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenApiFails_LogsErrorAndThrows()
    {
        // Arrange
        var symbol = "MSFT";
        var payload = new CompanyNewsAlertPayload { Symbol = symbol };
        _finnhubMock.Setup(f => f.GetCompanyNewsAsync(symbol, It.IsAny<string>(), It.IsAny<string>(), Ct))
            .ThrowsAsync(new Exception("API Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _sut.HandleAsync(payload, Ct));
    }
}
