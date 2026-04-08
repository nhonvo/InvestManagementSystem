using InventoryAlert.Contracts.Events.Payloads;
using InventoryAlert.Worker.Application.IntegrationHandlers;
using InventoryAlert.Worker.Infrastructure.External.Finnhub;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Worker.Handlers;

public class PriceAlertHandlerTests
{
    private readonly Mock<ILogger<PriceAlertHandler>> _loggerMock = new();
    private readonly Mock<IFinnhubClient> _finnhubMock = new();
    private readonly PriceAlertHandler _sut;

    public PriceAlertHandlerTests()
    {
        _sut = new PriceAlertHandler(_finnhubMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_FetchesQuote_AndLogsAlertMessage()
    {
        // Arrange
        var payload = new MarketPriceAlertPayload
        {
            ProductId = 1,
            Symbol = "MSFT"
        };

        _finnhubMock.Setup(f => f.FetchQuoteAsync("MSFT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FinnhubQuoteModel { CurrentPrice = 400.50m, PercentChange = -20.0m });

        // Act
        await _sut.HandleAsync(payload);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("PRICE ALERT") && v.ToString()!.Contains("MSFT")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
