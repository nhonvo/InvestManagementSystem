using FluentAssertions;
using InventoryAlert.Api.Services;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Application.Services;

public class NotificationServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly NotificationService _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;
    private const string UserIdStr = "00000000-0000-0000-0000-000000000001";
    private static readonly Guid UserId = Guid.Parse(UserIdStr);

    public NotificationServiceTests()
    {
        _uow.Setup(u => u.Notifications).Returns(new Mock<INotificationRepository>().Object);
        _sut = new NotificationService(_uow.Object);
    }

    [Fact]
    public async Task CreateAsync_ReturnsCorrectResponse()
    {
        // Arrange
        var message = "Threshold breached!";
        var symbol = "AAPL";

        // Act
        var result = await _sut.CreateAsync(UserId, message, symbol, null, Ct);

        // Assert
        result.Message.Should().Be(message);
        result.TickerSymbol.Should().Be(symbol);
        _uow.Verify(u => u.Notifications.AddAsync(It.IsAny<Notification>(), Ct), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }

    [Fact]
    public async Task GetUnreadCount_CallsRepository()
    {
        // Arrange
        _uow.Setup(u => u.Notifications.GetUnreadCountAsync(UserIdStr, Ct)).ReturnsAsync(5);

        // Act
        var result = await _sut.GetUnreadCountAsync(UserIdStr, Ct);

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    public async Task MarkRead_OnlyUpdatesIfOwnedByUser()
    {
        // Arrange
        var id = Guid.NewGuid();
        var notification = new Notification { Id = id, UserId = UserId, IsRead = false };
        _uow.Setup(u => u.Notifications.GetByIdAsync(id, Ct)).ReturnsAsync(notification);

        // Act
        await _sut.MarkReadAsync(id, UserIdStr, Ct);

        // Assert
        notification.IsRead.Should().BeTrue();
        _uow.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }
}
