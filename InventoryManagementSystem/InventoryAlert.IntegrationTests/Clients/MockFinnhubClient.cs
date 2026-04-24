using InventoryAlert.Domain.External.Finnhub;
using InventoryAlert.Domain.Interfaces;
using RestSharp;

namespace InventoryAlert.IntegrationTests.Clients;

public class MockFinnhubClient : IFinnhubClient
{
    private readonly RestClient _client;

    public MockFinnhubClient(RestClient client)
    {
        _client = client;
    }

    public Task<RestResponse> CheckWiremockAsync()
    {
        var request = new RestRequest("/some/thing");
        return _client.ExecuteGetAsync(request);
    }

    public Task<List<FinnhubNewsItem>> GetCompanyNewsAsync(string symbol, string from, string to, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<FinnhubEarnings>> GetEarningsAsync(string symbol, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<FinnhubEarningsCalendar?> GetEarningsCalendarAsync(string from, string to, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<FinnhubInsiderResponse?> GetInsidersAsync(string symbol, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<FinnhubIpoCalendar?> GetIpoCalendarAsync(string from, string to, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<FinnhubHoliday>> GetMarketHolidaysAsync(string exchange, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<FinnhubNewsItem>> GetMarketNewsAsync(string category, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<FinnhubMarketStatus?> GetMarketStatusAsync(string exchange, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<FinnhubMetricsResponse?> GetMetricsAsync(string symbol, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<string>> GetPeersAsync(string symbol, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<FinnhubProfileResponse?> GetProfileAsync(string symbol, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<FinnhubQuoteResponse?> GetQuoteAsync(string symbol, CancellationToken ct = default)
    {
        var request = new RestRequest("/quote");
        request.AddQueryParameter("symbol", symbol);
        request.AddHeader("X-Finnhub-Token", "mock-token");
        return _client.GetAsync<FinnhubQuoteResponse>(request);
    }

    public Task<List<FinnhubRecommendation>> GetRecommendationsAsync(string symbol, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<FinnhubSymbolSearch?> SearchSymbolsAsync(string query, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
