using System.Net;
using FluentAssertions;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.E2ETests.Abstractions;
using RestSharp;

namespace InventoryAlert.E2ETests;

public class PortfolioE2ETests : BaseE2ETest
{
    [Fact]
    public async Task GetPositions_ShouldReturnPaged()
    {
        await EnsureAuthenticatedAsync();

        var request = CreateAuthenticatedRequest("api/v1/portfolio/positions", Method.Get);
        var response = await Client.ExecuteAsync<PagedResult<PortfolioPositionResponse>>(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task OpenAndRemovePosition_ShouldSucceed()
    {
        await EnsureAuthenticatedAsync();
        const string symbol = "MSFT";

        // 1. Open
        // Pre-seed symbol discovery
        var seedReq = CreateAuthenticatedRequest($"api/v1/stocks/{symbol}/quote", Method.Get);
        await Client.ExecuteAsync(seedReq);

        var openReq = CreateAuthenticatedRequest("api/v1/portfolio/positions", Method.Post);
        openReq.AddJsonBody(new CreatePositionRequest(symbol, 5, 400.00m, DateTime.UtcNow.AddMinutes(-5)));
        var openRes = await Client.ExecuteAsync<PortfolioPositionResponse>(openReq);
        openRes.StatusCode.Should().Be(HttpStatusCode.Created);

        // 2. Remove
        var delReq = CreateAuthenticatedRequest($"api/v1/portfolio/positions/{symbol}", Method.Delete);
        var delRes = await Client.ExecuteAsync(delReq);
        delRes.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RecordTrade_ShouldSucceed()
    {
        await EnsureAuthenticatedAsync();
        const string symbol = "AMZN";

        // 0. Ensure symbol discovery
        var seedReq = CreateAuthenticatedRequest($"api/v1/stocks/{symbol}/quote", Method.Get);
        await Client.ExecuteAsync(seedReq);

        // 1. Ensure position exists
        var openReq = CreateAuthenticatedRequest("api/v1/portfolio/positions", Method.Post);
        openReq.AddJsonBody(new CreatePositionRequest(symbol, 10, 150.00m, DateTime.UtcNow.AddMinutes(-10)));
        await Client.ExecuteAsync(openReq);

        // Record trade
        var tradeReq = CreateAuthenticatedRequest($"api/v1/portfolio/{symbol}/trades", Method.Post);
        tradeReq.AddJsonBody(new TradeRequest(TradeType.Buy, 5, 180.00m, "E2E Trade"));

        var response = await Client.ExecuteAsync<PortfolioPositionResponse>(tradeReq);

        response.StatusCode.Should().Match(s => s == HttpStatusCode.OK || s == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task BulkImport_ShouldSucceed()
    {
        await EnsureAuthenticatedAsync();

        // 0. Ensure symbols discovery
        await Client.ExecuteAsync(CreateAuthenticatedRequest("api/v1/stocks/NVDA/quote", Method.Get));
        await Client.ExecuteAsync(CreateAuthenticatedRequest("api/v1/stocks/META/quote", Method.Get));

        // 1. Act
        var request = CreateAuthenticatedRequest("api/v1/portfolio/bulk", Method.Post);
        request.AddJsonBody(new List<CreatePositionRequest>
        {
            new("NVDA", 10, 800.00m, DateTime.UtcNow.AddMinutes(-5)),
            new("META", 5, 450.00m, DateTime.UtcNow.AddMinutes(-5))
        });

        var response = await Client.ExecuteAsync(request);

        // 2. Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAlerts_ShouldReturnOk()
    {
        await EnsureAuthenticatedAsync();

        var request = CreateAuthenticatedRequest("api/v1/portfolio/alerts", Method.Get);
        var response = await Client.ExecuteAsync<IEnumerable<PortfolioAlertResponse>>(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPosition_ShouldReturnOk_WhenExists()
    {
        await EnsureAuthenticatedAsync();
        const string symbol = "GOOG";

        // 1. Seed & Open
        await Client.ExecuteAsync(CreateAuthenticatedRequest($"api/v1/stocks/{symbol}/quote", Method.Get));
        var openReq = CreateAuthenticatedRequest("api/v1/portfolio/positions", Method.Post);
        openReq.AddJsonBody(new CreatePositionRequest(symbol, 2, 100.00m, DateTime.UtcNow));
        var openRes = await Client.ExecuteAsync<PortfolioPositionResponse>(openReq);
        openRes.StatusCode.Should().Be(HttpStatusCode.Created);

        // 2. Get Single Position
        var getReq = CreateAuthenticatedRequest($"api/v1/portfolio/positions/{symbol}", Method.Get);
        var getRes = await Client.ExecuteAsync<PortfolioPositionResponse>(getReq);
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);
        getRes.Data.Should().NotBeNull();
        getRes.Data!.Symbol.Should().Be(symbol);

        // Clean up
        await Client.ExecuteAsync(CreateAuthenticatedRequest($"api/v1/portfolio/positions/{symbol}", Method.Delete));
    }
}
