using InventoryAlert.Domain.DTOs;
using InventoryAlert.IntegrationTests.Abstractions;
using RestSharp;

namespace InventoryAlert.IntegrationTests.Clients;

public class WatchlistClient : BaseClient
{
    public WatchlistClient(RestClient client) : base(client)
    {
    }

    public async Task<RestResponse<List<PortfolioPositionResponse>>> GetWatchlistAsync(string accessToken)
    {
        var request = new RestRequest("/Watchlist");
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        return await _client.ExecuteGetAsync<List<PortfolioPositionResponse>>(request);
    }

    public async Task<RestResponse<PortfolioPositionResponse>> GetSingleWatchlistItemAsync(string accessToken, string symbol)
    {
        var request = new RestRequest("/Watchlist/{symbol}");
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        request.AddUrlSegment("symbol", symbol);
        return await _client.ExecuteGetAsync<PortfolioPositionResponse>(request);
    }

    public async Task<RestResponse<PortfolioPositionResponse>> AddToWatchlistAsync(string accessToken, string symbol)
    {
        var request = new RestRequest("/Watchlist/{symbol}");
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        request.AddUrlSegment("symbol", symbol);
        return await _client.ExecutePostAsync<PortfolioPositionResponse>(request);
    }

    public async Task<RestResponse> RemoveFromWatchlistAsync(string accessToken, string symbol)
    {
        var request = new RestRequest("/Watchlist/{symbol}");
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        request.AddUrlSegment("symbol", symbol);
        return await _client.ExecuteDeleteAsync(request);
    }
}
