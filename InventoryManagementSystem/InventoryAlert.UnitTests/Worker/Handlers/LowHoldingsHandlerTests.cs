using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Events.Payloads;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Worker.IntegrationEvents.Handlers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Worker.Handlers;

public class LowHoldingsHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IAlertNotifier> _notifierMock = new();
    private readonly Mock<ILogger<LowHoldingsHandler>> _loggerMock = new();
    private readonly LowHoldingsHandler _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public LowHoldingsHandlerTests()
    {
        _sut = new LowHoldingsHandler(_uowMock.Object, _notifierMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_CreatesNotification_AndNotifies()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var symbol = "AAPL";
        var payload = new LowHoldingsAlertPayload(userId, symbol, 10, 5);

        _uowMock.Setup(u => u.Notifications.AddAsync(It.IsAny<Notification>(), Ct))
            .ReturnsAsync(new Notification());

        // Act
        await _sut.HandleAsync(payload, Ct);

        // Assert
        _uowMock.Verify(u => u.Notifications.AddAsync(It.Is<Notification>(n =>
            n.UserId == userId && n.TickerSymbol == symbol && n.Message.Contains("Low holdings")), Ct), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
        _notifierMock.Verify(n => n.NotifyAsync(It.IsAny<Notification>(), Ct), Times.Once);
    }
}
