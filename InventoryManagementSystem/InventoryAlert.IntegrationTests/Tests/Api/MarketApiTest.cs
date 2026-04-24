using System.Net;
using FluentAssertions;
using InventoryAlert.IntegrationTests.Abstractions;
using InventoryAlert.IntegrationTests.Clients;
using InventoryAlert.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using Xunit.Abstractions;

namespace InventoryAlert.IntegrationTests.Tests.Api;

public class MarketApiTest : BaseIntegrationTest
{
    private readonly MarketClient _marketClient;
    private readonly AuthClient _authClient;
    
    public MarketApiTest(InjectionFixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        var restClient = fixture.ServiceProvider.GetRequiredService<RestClient>();
        _marketClient = new MarketClient(restClient);
        _authClient = new AuthClient(restClient);
    }

    [Fact]
    public async Task GetMarketStatus_ShouldReturnMarketStatus_Always()
    {
        // Arrange

        // Act
        var response = await _marketClient.GetMarketStatusAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        _output.WriteLine($"Market Status: {response.Content}");
    }

    [Fact]
    public async Task GetMarketNews_ShouldReturnMarketNews_WhenTokenIsValid()
    {
        // Arrange
        var loginResponse = await _authClient.LoginAsync(_testUser.Username, _testUser.Password);
        var accessToken = loginResponse.Data!.AccessToken;
        var category = "general";

        // Act
        var response = await _marketClient.GetMarketNewsAsync(accessToken, category);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMarketNews_ShouldReturnUnauthorized_WhenTokenIsInvalid()
    {
        // Arrange
        var invalidToken = "invalid_token";
        var category = "general";

        // Act
        var response = await _marketClient.GetMarketNewsAsync(invalidToken, category);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}