using System.Net;
using FluentAssertions;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.E2ETests.Abstractions;
using Microsoft.AspNetCore.SignalR.Client;
using RestSharp;
using Xunit;

namespace InventoryAlert.E2ETests;

public class SignalRNotificationE2ETests : BaseE2ETest
{
    [Fact]
    public async Task SignalR_ShouldReceiveRealTimeNotification()
    {
        // 1. Arrange - Authenticate and setup SignalR connection
        await EnsureAuthenticatedAsync();
        
        var hubUrl = $"{BaseUrl}{SignalRConstants.NotificationHubRoute}?access_token={JwtToken}";
        
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        NotificationResponse? receivedNotification = null;
        var tcs = new TaskCompletionSource<NotificationResponse>();

        connection.On<NotificationResponse>("ReceiveNotification", (notification) =>
        {
            receivedNotification = notification;
            tcs.SetResult(notification);
        });

        await connection.StartAsync();

        try
        {
            // 2. Act - Trigger notification via test endpoint
            var testMessage = $"E2E SignalR Test {Guid.NewGuid()}";
            var request = CreateAuthenticatedRequest($"api/v1/notifications/test-signalr?message={testMessage}", Method.Post);
            
            var response = await Client.ExecuteAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // 3. Assert - Wait for SignalR push
            var timeoutTask = Task.Delay(5000); // 5s timeout
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException("SignalR notification was not received within timeout.");
            }

            receivedNotification.Should().NotBeNull();
            receivedNotification!.Message.Should().Be(testMessage);
            receivedNotification.TickerSymbol.Should().Be("TEST");
        }
        finally
        {
            await connection.StopAsync();
            await connection.DisposeAsync();
        }
    }
}
