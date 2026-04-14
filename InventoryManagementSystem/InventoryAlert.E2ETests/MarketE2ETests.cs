using System.Net;
using FluentAssertions;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.E2ETests.Abstractions;
using RestSharp;

namespace InventoryAlert.E2ETests;

public class MarketE2ETests : BaseE2ETest
{
    [Fact]
    public async Task GetStatus_ShouldReturnMarketStatus()
    {
        // 1. Act (Anonymous allowed on status)
        var request = new RestRequest("api/v1/market/status", Method.Get);
        var response = await Client.ExecuteAsync<MarketStatusResponse>(request);

        // 2. Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetQuote_ShouldReturnQuote_AfterEnsuringSymbolExists()
    {
        // 1. Ensure authenticated
        await EnsureAuthenticatedAsync();

        // 2. Resolve a symbol from the catalog first to ensure we have a valid test case
        var catalogReq = CreateAuthenticatedRequest("api/v1/stocks", Method.Get);
        var catalogRes = await Client.ExecuteAsync<PagedResult<StockProfileResponse>>(catalogReq);

        catalogRes.StatusCode.Should().Be(HttpStatusCode.OK);
        catalogRes.Data.Should().NotBeNull();

        var symbol = catalogRes.Data!.Items.FirstOrDefault()?.Symbol ?? "AAPL";

        // 3. Pre-seed/Visit the symbol
        var preSeedReq = CreateAuthenticatedRequest($"api/v1/stocks/{symbol}/quote", Method.Get);
        await Client.ExecuteAsync(preSeedReq);

        // 4. Act
        var request = CreateAuthenticatedRequest($"api/v1/stocks/{symbol}/quote", Method.Get);
        var response = await Client.ExecuteAsync<StockQuoteResponse>(request);

        // 5. Assert
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            // Finnhub trial key or Moto might return 404 for some symbols
            // But if it's in the catalog, it's usually expected to work
            // Let's just log it if it fails instead of failing the test if symbol is from catalog
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"Symbol '{symbol}' was found in catalog but quote returned 404. Check Finnhub key.");
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Should().NotBeNull();
            response.Data!.Symbol.Should().Be(symbol);
        }
    }
}
