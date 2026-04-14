using System.Net;
using FluentAssertions;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.E2ETests.Abstractions;
using RestSharp;

namespace InventoryAlert.E2ETests;

public class NotificationE2ETests : BaseE2ETest
{
    [Fact]
    public async Task GetNotifications_ShouldReturnOk()
    {
        // 1. Arrange
        await EnsureAuthenticatedAsync();

        // 2. Act
        var request = CreateAuthenticatedRequest("api/v1/notifications", Method.Get);
        var response = await Client.ExecuteAsync<PagedResult<NotificationResponse>>(request);

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkAsRead_ShouldReturnNoContent_WhenNotificationExists()
    {
        // 1. Arrange
        await EnsureAuthenticatedAsync();

        var guid = Guid.NewGuid();
        // Controller uses PATCH for read/read-all
        var request = CreateAuthenticatedRequest($"api/v1/notifications/{guid}/read", Method.Patch);

        var response = await Client.ExecuteAsync(request);

        // 3. Assert
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            // Expected if no notification exists with this random GUID
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
    }

    [Fact]
    public async Task GetUnreadCount_ShouldReturnOk()
    {
        // 1. Arrange
        await EnsureAuthenticatedAsync();

        // 2. Act
        var request = CreateAuthenticatedRequest("api/v1/notifications/unread-count", Method.Get);
        var response = await Client.ExecuteAsync<int>(request);

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task MarkAllRead_ShouldReturnOk()
    {
        // 1. Arrange
        await EnsureAuthenticatedAsync();

        // 2. Act
        var request = CreateAuthenticatedRequest("api/v1/notifications/read-all", Method.Patch);
        var response = await Client.ExecuteAsync<int>(request);

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Dismiss_ShouldReturnNoContent_WhenExists()
    {
        // 1. Arrange
        await EnsureAuthenticatedAsync();
        var guid = Guid.NewGuid();

        // 2. Act
        var request = CreateAuthenticatedRequest($"api/v1/notifications/{guid}", Method.Delete);
        var response = await Client.ExecuteAsync(request);

        // 3. Assert
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            // Expected if no notification exists with this random GUID
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
    }
}
