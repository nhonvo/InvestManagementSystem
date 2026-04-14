using System.Net;
using FluentAssertions;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.E2ETests.Abstractions;
using RestSharp;

namespace InventoryAlert.E2ETests;

public class AlertRulesE2ETests : BaseE2ETest
{
    [Fact]
    public async Task CreateAlert_ShouldReturnCreated_WhenSymbolIsResolved()
    {
        // 1. Arrange
        await EnsureAuthenticatedAsync();

        // 2. Pre-resolve NVDA in local cache
        var seedReq = CreateAuthenticatedRequest("api/v1/stocks/DIS/quote", Method.Get);
        await Client.ExecuteAsync(seedReq);

        // 3. Act
        var request = CreateAuthenticatedRequest("api/v1/alertrules", Method.Post);
        // Corrected enum value
        request.AddJsonBody(new AlertRuleRequest("DIS", AlertCondition.PriceAbove, 200.00m, true));

        var response = await Client.ExecuteAsync<AlertRuleResponse>(request);

        // 4. Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Data.Should().NotBeNull();
        response.Data!.TickerSymbol.Should().Be("DIS");
    }

    [Fact]
    public async Task GetAlerts_ShouldReturnList()
    {
        // 1. Arrange
        await EnsureAuthenticatedAsync();

        // 2. Act
        var request = CreateAuthenticatedRequest("api/v1/alertrules", Method.Get);
        var response = await Client.ExecuteAsync<IEnumerable<AlertRuleResponse>>(request);

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAlert_ShouldReturnOk()
    {
        // 1. Arrange
        await EnsureAuthenticatedAsync();
        var createReq = CreateAuthenticatedRequest("api/v1/alertrules", Method.Post);
        createReq.AddJsonBody(new AlertRuleRequest("DIS", AlertCondition.PriceAbove, 200.00m, true));
        var createRes = await Client.ExecuteAsync<AlertRuleResponse>(createReq);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var alertId = createRes.Data!.Id;

        // 2. Act
        var updateReq = CreateAuthenticatedRequest($"api/v1/alertrules/{alertId}", Method.Put);
        updateReq.AddJsonBody(new AlertRuleRequest("DIS", AlertCondition.PriceBelow, 150.00m, false));
        var updateRes = await Client.ExecuteAsync<AlertRuleResponse>(updateReq);

        // 3. Assert
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);
        updateRes.Data!.TargetValue.Should().Be(150.00m);
        updateRes.Data!.Condition.Should().Be(AlertCondition.PriceBelow);
    }

    [Fact]
    public async Task ToggleAlert_ShouldReturnOk()
    {
        // 1. Arrange
        await EnsureAuthenticatedAsync();
        var createReq = CreateAuthenticatedRequest("api/v1/alertrules", Method.Post);
        createReq.AddJsonBody(new AlertRuleRequest("DIS", AlertCondition.PriceAbove, 200.00m, true));
        var createRes = await Client.ExecuteAsync<AlertRuleResponse>(createReq);
        var alertId = createRes.Data!.Id;

        // 2. Act
        var toggleReq = CreateAuthenticatedRequest($"api/v1/alertrules/{alertId}/toggle", Method.Patch);
        toggleReq.AddJsonBody(new ToggleAlertRequest(false));
        var toggleRes = await Client.ExecuteAsync<AlertRuleResponse>(toggleReq);

        // 3. Assert
        toggleRes.StatusCode.Should().Be(HttpStatusCode.OK);
        toggleRes.Data!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAlert_ShouldReturnNoContent()
    {
        // 1. Arrange
        await EnsureAuthenticatedAsync();
        var createReq = CreateAuthenticatedRequest("api/v1/alertrules", Method.Post);
        createReq.AddJsonBody(new AlertRuleRequest("DIS", AlertCondition.PriceAbove, 200.00m, true));
        var createRes = await Client.ExecuteAsync<AlertRuleResponse>(createReq);
        var alertId = createRes.Data!.Id;

        // 2. Act
        var deleteReq = CreateAuthenticatedRequest($"api/v1/alertrules/{alertId}", Method.Delete);
        var deleteRes = await Client.ExecuteAsync(deleteReq);

        // 3. Assert
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
