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

namespace InventoryAlert.IntegrationTests.Tests.Worker.Jobs;

public class NewsSyncJobTest : BaseIntegrationTest
{
    private readonly EventClient _eventClient;
    private readonly AuthClient _authClient;
    private readonly MarketClient _marketClient;

    public NewsSyncJobTest(InjectionFixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        var restClient = fixture.ServiceProvider.GetRequiredService<RestClient>();
        _eventClient = new EventClient(restClient);
        _authClient = new AuthClient(restClient);
        _marketClient = new MarketClient(restClient);
    }

    [Fact]
    public async Task RequestingNewsSync_ShouldTriggerJobAndPopulateData()
    {
        // Arrange
        var loginResponse = await _authClient.LoginAsync(_testUser.Username, _testUser.Password);
        var token = loginResponse.Data!.AccessToken;

        // Act
        var response = await _eventClient.PublishEventAsync(token, EventTypes.SyncMarketNewsRequested, new { });
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        
        await Task.Delay(3000); // Give worker time to process

        var newsResponse = await _marketClient.GetMarketNewsAsync(token, "general");
        newsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
