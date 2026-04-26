using System.Security.Claims;
using FluentAssertions;
using InventoryAlert.Api.Controllers;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Web.Controllers;

public class NotificationsControllerTests
{
    private readonly Mock<INotificationService> _service = new();
    private readonly Mock<IAlertNotifier> _notifier = new();
    private readonly NotificationsController _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;
    private const string UserId = "user-1";

    public NotificationsControllerTests()
    {
        _sut = new NotificationsController(_service.Object, _notifier.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
            new(ClaimTypes.NameIdentifier, UserId),
        }, "mock"));

        _sut.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };
    }

    [Fact]
    public async Task GetNotifications_Returns200_WithPagedItems()
    {
        // Arrange
        var pagedResult = new PagedResult<NotificationResponse>
        {
            Items = new List<NotificationResponse>(),
            TotalItems = 0,
            PageNumber = 1,
            PageSize = 10
        };
        _service.Setup(s => s.GetPagedAsync(UserId, false, 1, 10, Ct)).ReturnsAsync(pagedResult);

        // Act
        var result = await _sut.GetNotifications(false, 1, 10, Ct);

        // Assert
        var ok = (result.Result as OkObjectResult);
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().BeEquivalentTo(pagedResult);
    }

    [Fact]
    public async Task GetUnreadCount_Returns200_WithCount()
    {
        // Arrange
        _service.Setup(s => s.GetUnreadCountAsync(UserId, Ct)).ReturnsAsync(5);

        // Act
        var result = await _sut.GetUnreadCount(Ct);

        // Assert
        var ok = (result.Result as OkObjectResult);
        ok.Should().NotBeNull();
        ok!.Value.Should().Be(5);
    }

    [Fact]
    public async Task MarkReadAll_Returns200_WithUpdatedCount()
    {
        // Arrange
        _service.Setup(s => s.MarkAllReadAsync(UserId, Ct)).ReturnsAsync(10);

        // Act
        var result = await _sut.MarkAllRead(Ct);

        // Assert
        var ok = (result.Result as OkObjectResult);
        ok.Should().NotBeNull();
        ok!.Value.Should().Be(10);
    }
}
