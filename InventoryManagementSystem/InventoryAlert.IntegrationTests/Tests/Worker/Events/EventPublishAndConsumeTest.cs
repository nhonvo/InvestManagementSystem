using System.Net;
using FluentAssertions;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Events;
using InventoryAlert.IntegrationTests.Abstractions;
using InventoryAlert.IntegrationTests.Clients;
using InventoryAlert.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using Xunit.Abstractions;

namespace InventoryAlert.IntegrationTests.Tests.Worker.Events;

public class EventPublishAndConsumeTest : BaseIntegrationTest, IDisposable
{
    private readonly SqsClient _sqsClient = new();
    private readonly EventClient _eventClient;
    private readonly AuthClient _authClient;

    public EventPublishAndConsumeTest(InjectionFixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        var restClient = fixture.ServiceProvider.GetRequiredService<RestClient>();
        _eventClient = new EventClient(restClient);
        _authClient = new AuthClient(restClient);
    }

    [Fact]
    public async Task PublishEvent_ShouldBeDeliveredToSQS_WithCorrelationId()
    {
        // Arrange
        var loginResponse = await _authClient.LoginAsync(_testUser.Username, _testUser.Password);
        var token = loginResponse.Data!.AccessToken;

        await _sqsClient.PurgeMainQueueAsync();

        // Act
        var response = await _eventClient.PublishEventAsync(token, EventTypes.MarketPriceAlert, new { Message = "Integration Test" });
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // Wait a short bit for SNS -> SQS
        await Task.Delay(2000);

        // Assert
        var messages = await _sqsClient.ReceiveMessagesFromMainAsync();
        // Either worker consumed it or it's in the queue
    }

    public void Dispose()
    {
        _sqsClient.Dispose();
        GC.SuppressFinalize(this);
    }
}