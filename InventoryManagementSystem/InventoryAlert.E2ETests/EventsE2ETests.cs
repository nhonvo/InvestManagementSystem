using System.Net;
using FluentAssertions;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Events;
using InventoryAlert.E2ETests.Abstractions;
using RestSharp;

namespace InventoryAlert.E2ETests;

public class EventsE2ETests : BaseE2ETest
{
    [Fact]
    public async Task PublishEvent_ShouldSucceed_ForValidEventType()
    {
        // 1. Arrange
        await EnsureAuthenticatedAsync();

        // 2. Act
        var request = CreateAuthenticatedRequest("api/v1/events", Method.Post);
        request.AddJsonBody(new PublishEventRequest
        {
            EventType = EventTypes.SyncMarketNewsRequested, // Constant from domain
            Payload = new
            {
                Reason = "E2E xUnit Test",
                Category = "general",
                Source = "Test Runner",
                Headline = "Successful E2E Dispatch"
            }
        });

        var response = await Client.ExecuteAsync(request);

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }
}
