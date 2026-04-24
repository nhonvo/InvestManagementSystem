using InventoryAlert.Domain.DTOs;
using InventoryAlert.IntegrationTests.Abstractions;
using RestSharp;

namespace InventoryAlert.IntegrationTests.Clients;

public class StockClient : BaseClient
{
    public StockClient(RestClient client) : base(client)
    {
    }

    public async Task<RestResponse<PagedResult<StockProfileResponse>>> GetStocksAsync(string accessToken, int? page = null, int? pageSize = null, string? exchange = null, string? industry = null)
    {
        var request = new RestRequest("/Stocks");
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        if (page.HasValue)
            request.AddParameter("page", page.Value);
        if (pageSize.HasValue)
            request.AddParameter("pageSize", pageSize.Value);
        if (!string.IsNullOrEmpty(exchange))
            request.AddParameter("exchange", exchange);
        if (!string.IsNullOrEmpty(industry))
            request.AddParameter("industry", industry);
        return await _client.ExecuteGetAsync<PagedResult<StockProfileResponse>>(request);
    }

    public async Task<RestResponse<SymbolSearchResponse>> SearchStockAsync(string accessToken, string q)
    {
        var request = new RestRequest("/Stocks");
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        request.AddQueryParameter("q", q);
        return await _client.ExecuteGetAsync<SymbolSearchResponse>(request);
    }

    public async Task<RestResponse<StockQuoteResponse>> GetStockQuoteAsync(string accessToken, string symbol)
    {
        var request = new RestRequest("/Stocks/{symbol}/quote");
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        request.AddUrlSegment("symbol", symbol);
        return await _client.ExecuteGetAsync<StockQuoteResponse>(request);
    }

    public async Task<RestResponse<StockProfileResponse>> GetStockProfile(string accessToken, string symbol)
    {
        var request = new RestRequest("/Stocks/{symbol}/profile");
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        request.AddUrlSegment("symbol", symbol);
        return await _client.ExecuteGetAsync<StockProfileResponse>(request);
    }

    public async Task<RestResponse<StockMetricResponse>> GetStockFinancials(string accessToken, string symbol)
    {
        var request = new RestRequest("/Stocks/{symbol}/financials");
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        request.AddUrlSegment("symbol", symbol);
        return await _client.ExecuteGetAsync<StockMetricResponse>(request);
    }

    public async Task<RestResponse<EarningsSurpriseResponse>> GetStockEarnings(string accessToken, string symbol)
    {
        var request = new RestRequest("/Stocks/{symbol}/earnings");
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        request.AddUrlSegment("symbol", symbol);
        return await _client.ExecuteGetAsync<EarningsSurpriseResponse>(request);
    }

    public async Task<RestResponse<RecommendationResponse>> GetStockRecommendation(string accessToken, string symbol)
    {
        var request = new RestRequest("/Stocks/{symbol}/recommendation");
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        request.AddUrlSegment("symbol", symbol);
        return await _client.ExecuteGetAsync<RecommendationResponse>(request);
    }

    public async Task<RestResponse<InsiderTransactionResponse>> GetStockInsiders(string accessToken, string symbol)
    {
        var request = new RestRequest("/Stocks/{symbol}/insiders");
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        request.AddUrlSegment("symbol", symbol);
        return await _client.ExecuteGetAsync<InsiderTransactionResponse>(request);
    }

    public async Task<RestResponse<PeersResponse>> GetStockPeers(string accessToken, string symbol)
    {
        var request = new RestRequest("/Stocks/{symbol}/peers");
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        request.AddUrlSegment("symbol", symbol);
        return await _client.ExecuteGetAsync<PeersResponse>(request);
    }

    public async Task<RestResponse<NewsResponse>> GetStockNews(string accessToken, string symbol, int? page, int? pageSize)
    {
        var request = new RestRequest("/Stocks/{symbol}/news");
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        request.AddUrlSegment("symbol", symbol);
        if (page.HasValue)
            request.AddParameter("page", page.Value);
        if (pageSize.HasValue)
            request.AddParameter("pageSize", pageSize.Value);
        return await _client.ExecuteGetAsync<NewsResponse>(request);
    }
}
