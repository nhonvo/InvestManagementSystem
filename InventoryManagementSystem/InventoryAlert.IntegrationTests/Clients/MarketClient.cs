using InventoryAlert.Domain.DTOs;
using InventoryAlert.IntegrationTests.Abstractions;
using RestSharp;

namespace InventoryAlert.IntegrationTests.Clients;

public class MarketClient : BaseClient
{
    public MarketClient(RestClient client) : base(client)
    {
    }

    public async Task<RestResponse<List<MarketStatusResponse>>> GetMarketStatusAsync()
    {
        var request = new RestRequest("/Market/status");
        return await _client.ExecuteAsync<List<MarketStatusResponse>>(request);
    }

    public async Task<RestResponse<List<MarketHolidayResponse>>> GetMarketHolidaysAsync(string accessToken, string exchange)
    {
        var request = new RestRequest("/Market/holidays");
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        request.AddQueryParameter("exchange", exchange);
        return await _client.ExecuteAsync<List<MarketHolidayResponse>>(request);
    }

    public async Task<RestResponse<List<NewsResponse>>> GetMarketNewsAsync(string accessToken, string category)
    {
        var request = new RestRequest("/Market/news");
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        request.AddQueryParameter("category", category);
        return await _client.ExecuteAsync<List<NewsResponse>>(request);
    }

    public async Task<RestResponse<List<EarningsCalendarResponse>>> GetMarketCalendarEarningsAsync(string accessToken, string symbol)
    {
        var request = new RestRequest("/Market/calendar/earnings");
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        request.AddQueryParameter("symbol", symbol);
        return await _client.ExecuteAsync<List<EarningsCalendarResponse>>(request);
    }

    public async Task<RestResponse<List<IpoCalendarResponse>>> GetMarketCalendarIpoAsync(string accessToken, string symbol)
    {
        var request = new RestRequest("/Market/calendar/ipo");
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        request.AddQueryParameter("symbol", symbol);
        return await _client.ExecuteAsync<List<IpoCalendarResponse>>(request);
    }
}
