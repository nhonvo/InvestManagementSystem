using InventoryAlert.Domain.DTOs;
using InventoryAlert.IntegrationTests.Abstractions;
using RestSharp;

namespace InventoryAlert.IntegrationTests.Clients;

public class StockClient : BaseClient
{
    public StockClient(RestClient client) : base(client)
    {
    }

    public async Task<RestResponse<PagedResult<StockProfileResponse>>> GetStocksAsync(string token)
    {
        var request = new RestRequest("/Stocks");
        request.AddHeader("Authorization", $"Bearer {token}");
        return await _client.ExecuteGetAsync<PagedResult<StockProfileResponse>>(request);
    }

    public async Task<RestResponse<StockQuoteResponse>> GetStockQuoteAsync(string token, string symbol)
    {
        var request = new RestRequest("/Stocks/{symbol}/quote");
        request.AddHeader("Authorization", $"Bearer {token}");
        request.AddUrlSegment("symbol", symbol);
        return await _client.ExecuteGetAsync<StockQuoteResponse>(request);
    }
}
