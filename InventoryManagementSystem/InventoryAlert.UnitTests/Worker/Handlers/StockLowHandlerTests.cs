using InventoryAlert.Contracts.Events.Payloads;
using InventoryAlert.Contracts.Persistence.Interfaces;
using InventoryAlert.Worker.Application.IntegrationHandlers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Worker.Handlers;

public class StockLowHandlerTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<ILogger<StockLowHandler>> _loggerMock = new();
    private readonly StockLowHandler _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public StockLowHandlerTests()
    {
        _sut = new StockLowHandler(_productRepoMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_LogsAlert_WhenStockIsBelowPayloadThreshold()
    {
        // Arrange
        var product = new Product { Id = 1, TickerSymbol = "AAPL", StockCount = 5 };
        _productRepoMock.Setup(r => r.GetByIdAsync(1, Ct)).ReturnsAsync(product);

        var payload = new StockLowAlertPayload
        {
            ProductId = 1,
            Symbol = "AAPL",
            Threshold = 8 // Stock is 5, so 5 <= 8 should trigger
        };

        // Act
        await _sut.HandleAsync(payload, Ct);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("VERIFIED LOW STOCK") && v.ToString()!.Contains("AAPL")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_LogsInfo_WhenStockIsAbovePayloadThreshold()
    {
        // Arrange
        var product = new Product { Id = 1, TickerSymbol = "AAPL", StockCount = 15 };
        _productRepoMock.Setup(r => r.GetByIdAsync(1, Ct)).ReturnsAsync(product);

        var payload = new StockLowAlertPayload
        {
            ProductId = 1,
            Symbol = "AAPL",
            Threshold = 10 // Stock is 15, so 15 > 10 should NOT trigger
        };

        // Act
        await _sut.HandleAsync(payload, Ct);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stock level for AAPL has recovered")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
