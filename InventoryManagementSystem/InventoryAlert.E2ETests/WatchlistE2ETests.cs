using System.Net;
using FluentAssertions;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.E2ETests.Abstractions;
using RestSharp;

namespace InventoryAlert.E2ETests;

public class WatchlistE2ETests : BaseE2ETest
{
    [Fact]
    public async Task AddToWatchlist_ShouldSucceed_ForValidSymbol()
    {
        // 1. Arrange
        await EnsureAuthenticatedAsync();
        const string symbol = "DASH";

        // Cleanup to ensure repeatable test runs
        var cleanupReq = CreateAuthenticatedRequest($"api/v1/watchlist/{symbol}", Method.Delete);
        await Client.ExecuteAsync(cleanupReq);

        // 2. Act
        var request = CreateAuthenticatedRequest($"api/v1/watchlist/{symbol}", Method.Post);
        var response = await Client.ExecuteAsync<PortfolioPositionResponse>(request);

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Data.Should().NotBeNull();
        response.Data!.Symbol.Should().Be(symbol);
    }

    [Fact]
    public async Task GetWatchlist_ShouldReturnList()
    {
        // 1. Arrange
        await EnsureAuthenticatedAsync();

        // 2. Pre-condition: ensure at least one item
        var addReq = CreateAuthenticatedRequest("api/v1/watchlist/PYPL", Method.Post);
        await Client.ExecuteAsync(addReq);

        // 3. Act
        var request = CreateAuthenticatedRequest("api/v1/watchlist", Method.Get);
        var response = await Client.ExecuteAsync<IEnumerable<PortfolioPositionResponse>>(request);

        // 4. Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data.Should().Contain(w => w.Symbol == "PYPL");
    }

    [Fact]
    public async Task RemoveFromWatchlist_ShouldSucceed()
    {
        // 1. Arrange
        await EnsureAuthenticatedAsync();
        const string symbol = "ADBE";
        await Client.ExecuteAsync(CreateAuthenticatedRequest($"api/v1/watchlist/{symbol}", Method.Post));

        // 2. Act
        var request = CreateAuthenticatedRequest($"api/v1/watchlist/{symbol}", Method.Delete);
        var response = await Client.ExecuteAsync(request);

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetWatchlistItem_ShouldReturnItem_WhenExists()
    {
        // 1. Arrange
        await EnsureAuthenticatedAsync();
        const string symbol = "CSCO";
        await Client.ExecuteAsync(CreateAuthenticatedRequest($"api/v1/watchlist/{symbol}", Method.Post));

        // 2. Act
        var request = CreateAuthenticatedRequest($"api/v1/watchlist/{symbol}", Method.Get);
        var response = await Client.ExecuteAsync<PortfolioPositionResponse>(request);

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Symbol.Should().Be(symbol);

        // Cleanup
        await Client.ExecuteAsync(CreateAuthenticatedRequest($"api/v1/watchlist/{symbol}", Method.Delete));
    }
}
