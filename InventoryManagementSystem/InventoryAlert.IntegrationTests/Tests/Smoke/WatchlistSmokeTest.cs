using System.Net;
using FluentAssertions;
using InventoryAlert.IntegrationTests.Abstractions;
using InventoryAlert.IntegrationTests.Clients;
using InventoryAlert.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using Xunit.Abstractions;

namespace InventoryAlert.IntegrationTests.Tests.Smoke;

public class WatchlistSmokeTest : BaseIntegrationTest
{
    private readonly WatchlistClient _watchlistClient;
    private readonly AuthClient _authClient;

    public WatchlistSmokeTest(InjectionFixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        var restClient = fixture.ServiceProvider.GetRequiredService<RestClient>();
        _watchlistClient = new WatchlistClient(restClient);
        _authClient = new AuthClient(restClient);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task GetWatchlist_ShouldReturnWatchlist()
    {
        // Arrange
        var loginResponse = await _authClient.LoginAsync(_testUser.Username, _testUser.Password);
        var accessToken = loginResponse.Data!.AccessToken;

        // Act
        var response = await _watchlistClient.GetWatchlistAsync(accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task GetWatchlist_ShouldNotCrash_WhenUserIsNotAuthenticated()
    {
        // Arrange

        // Act
        var response = await _watchlistClient.GetWatchlistAsync("invalid_token");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
