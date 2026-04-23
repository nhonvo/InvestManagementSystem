using System.Net;
using FluentAssertions;
using InventoryAlert.IntegrationTests.Abstractions;
using InventoryAlert.IntegrationTests.Clients;
using InventoryAlert.IntegrationTests.Fixtures;
using InventoryAlert.IntegrationTests.TestUtils.Assertions;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using Xunit.Abstractions;

namespace InventoryAlert.IntegrationTests.Tests.Api;

public class WatchlistApiTest : BaseIntegrationTest
{
    private readonly WatchlistClient _watchlistClient;
    private readonly AuthClient _authClient;
    
    public WatchlistApiTest(InjectionFixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        var restClient = fixture.ServiceProvider.GetRequiredService<RestClient>();
        _watchlistClient = new WatchlistClient(restClient);
        _authClient = new AuthClient(restClient);
    }

    [Fact]
    public async Task GetWatchlist_ShouldReturnWatchlist_WhenUserIsAuthenticated()
    {
        // Arrange
        var loginResponse = await _authClient.LoginAsync(_testUser.Username, _testUser.Password);
        var accessToken = loginResponse.Data!.AccessToken;

        // Act
        var response = await _watchlistClient.GetWatchlistAsync(accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data.Should().AllSatisfy(item => WatchlistItemAssertion.AssertAllFieldsNotNull(item));
    }

    [Fact]
    public async Task GetWatchlist_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange

        // Act
        var response = await _watchlistClient.GetWatchlistAsync("invalid_token");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSingleWatchlistItem_ShouldReturnItem_WhenUserIsAuthenticated()
    {
        // Arrange
        string symbol = "TSLA";

        var loginResponse = await _authClient.LoginAsync(_testUser.Username, _testUser.Password);
        var accessToken = loginResponse.Data!.AccessToken;

        await _watchlistClient.AddToWatchlistAsync(accessToken, symbol);

        try
        {
            // Act
            var response = await _watchlistClient.GetSingleWatchlistItemAsync(accessToken, symbol);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Should().NotBeNull();
            WatchlistItemAssertion.AssertAllFieldsNotNull(response.Data);
            response.Data.Symbol.Should().Be(symbol);
        }
        finally
        {
            await _watchlistClient.RemoveFromWatchlistAsync(accessToken, symbol);
        }
    }

    [Fact]
    public async Task GetSingleWatchlistItem_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange

        // Act
        var response = await _watchlistClient.GetSingleWatchlistItemAsync("invalid_token", "AAPL");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSingleWatchlistItem_ShouldReturnNotFound_WhenItemDoesNotExist()
    {
        // Arrange
        var loginResponse = await _authClient.LoginAsync(_testUser.Username, _testUser.Password);
        var accessToken = loginResponse.Data!.AccessToken;

        // Act
        var response = await _watchlistClient.GetSingleWatchlistItemAsync(accessToken, "NON_EXISTENT_SYMBOL");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddToWatchlist_ShouldAddItem_WhenUserIsAuthenticated()
    {
        // Arrange
        string symbol = "MSFT";

        var loginResponse = await _authClient.LoginAsync(_testUser.Username, _testUser.Password);
        var accessToken = loginResponse.Data!.AccessToken;

        try
        {
            // Ensure the item is not already in the watchlist
            await _watchlistClient.RemoveFromWatchlistAsync(accessToken, symbol);
        }
        catch { /* Ignore if it doesn't exist */ }

        // Act
        var response = await _watchlistClient.AddToWatchlistAsync(accessToken, symbol);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Data.Should().NotBeNull();
        response.Data.Symbol.Should().Be(symbol);
        _output.WriteLine(response.Content);
    }

    [Fact]
    public async Task AddToWatchlist_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        string symbol = "MSFT";

        // Act
        var response = await _watchlistClient.AddToWatchlistAsync("invalid_token", symbol);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddToWatchList_ShouldReturnNotFound_WhenSymbolDoesNotExist()
    {
        // Arrange
        string symbol = "NON_EXIST_SYMBOL";

        var loginResponse = await _authClient.LoginAsync(_testUser.Username, _testUser.Password);
        var accessToken = loginResponse.Data!.AccessToken;

        try
        {
            // Ensure the item is not already in the watchlist
            await _watchlistClient.RemoveFromWatchlistAsync(accessToken, symbol);
        }
        catch { /* Ignore if it doesn't exist */ }

        // Act
        var response = await _watchlistClient.AddToWatchlistAsync(accessToken, symbol);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddToWatchlist_ShouldReturnBadRequest_WhenItemIsAlreadyInWatchlist()
    {
        // Arrange
        string symbol = "MSFT";

        var loginResponse = await _authClient.LoginAsync(_testUser.Username, _testUser.Password);
        var accessToken = loginResponse.Data!.AccessToken;

        try
        {
            // Ensure the item is not already in the watchlist
            await _watchlistClient.RemoveFromWatchlistAsync(accessToken, symbol);
        }
        catch { /* Ignore if it doesn't exist */ }

        var firstResponse = await _watchlistClient.AddToWatchlistAsync(accessToken, symbol); // Add the first one

        // Act
        var secondResponse = await _watchlistClient.AddToWatchlistAsync(accessToken, symbol);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RemoveFromWatchlist_ShouldRemoveItem_WhenUserIsAuthenticated()
    {
        // Arrange
        string symbol = "GOOGL";

        var loginResponse = await _authClient.LoginAsync(_testUser.Username, _testUser.Password);
        var accessToken = loginResponse.Data!.AccessToken;

        // Ensure the item is in the watchlist
        await _watchlistClient.AddToWatchlistAsync(accessToken, symbol);

        // Act
        var response = await _watchlistClient.RemoveFromWatchlistAsync(accessToken, symbol);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RemoveFromWatchlist_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        string symbol = "GOOGL";

        // Act
        var response = await _watchlistClient.RemoveFromWatchlistAsync("invalid_token", symbol);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RemoveFromWatchlist_ShouldReturnNotFound_WhenItemIsNotInWatchlist()
    {
        // Arrange
        string symbol = "GOOGL";

        var loginResponse = await _authClient.LoginAsync(_testUser.Username, _testUser.Password);
        var accessToken = loginResponse.Data!.AccessToken;

        // Act
        var response = await _watchlistClient.RemoveFromWatchlistAsync(accessToken, symbol);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
