using System.Net;
using FluentAssertions;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.E2ETests.Abstractions;
using RestSharp;

namespace InventoryAlert.E2ETests;

public class StocksE2ETests : BaseE2ETest
{
    [Fact]
    public async Task GetCatalog_ShouldReturnPagedResult()
    {
        await EnsureAuthenticatedAsync();

        var request = CreateAuthenticatedRequest("api/v1/stocks", Method.Get);
        var response = await Client.ExecuteAsync<PagedResult<StockProfileResponse>>(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task Search_ShouldReturnResults()
    {
        await EnsureAuthenticatedAsync();

        var request = CreateAuthenticatedRequest("api/v1/stocks/search", Method.Get);
        request.AddQueryParameter("q", "Apple");

        var response = await Client.ExecuteAsync<IEnumerable<SymbolSearchResponse>>(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProfileAndFinancials_ShouldReturnOk_WhenSymbolExists()
    {
        await EnsureAuthenticatedAsync();

        // Resolve a symbol from catalog
        var catalogReq = CreateAuthenticatedRequest("api/v1/stocks", Method.Get);
        var catalogRes = await Client.ExecuteAsync<PagedResult<StockProfileResponse>>(catalogReq);
        var symbol = catalogRes.Data?.Items.FirstOrDefault()?.Symbol ?? "AAPL";

        // 1. Profile
        var profileReq = CreateAuthenticatedRequest($"api/v1/stocks/{symbol}/profile", Method.Get);
        var profileRes = await Client.ExecuteAsync<StockProfileResponse>(profileReq);
        profileRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Financials
        var finReq = CreateAuthenticatedRequest($"api/v1/stocks/{symbol}/financials", Method.Get);
        var finRes = await Client.ExecuteAsync<StockMetricResponse>(finReq);
        finRes.StatusCode.Should().Match(s => s == HttpStatusCode.OK || s == HttpStatusCode.NotFound);

        // 3. Earnings
        var earnReq = CreateAuthenticatedRequest($"api/v1/stocks/{symbol}/earnings", Method.Get);
        var earnRes = await Client.ExecuteAsync<IEnumerable<EarningsSurpriseResponse>>(earnReq);
        earnRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
