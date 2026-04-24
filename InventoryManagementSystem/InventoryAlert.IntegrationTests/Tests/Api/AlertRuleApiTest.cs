using System.Net;
using FluentAssertions;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.IntegrationTests.Abstractions;
using InventoryAlert.IntegrationTests.Clients;
using InventoryAlert.IntegrationTests.Fixtures;
using InventoryAlert.IntegrationTests.TestUtils.Assertions;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using Xunit.Abstractions;

namespace InventoryAlert.IntegrationTests.Tests.Api;

public class AlertRuleApiTest : BaseIntegrationTest
{
    private readonly AlertRuleClient _alertRuleClient;
    private readonly AuthClient _authClient;
    
    public AlertRuleApiTest(InjectionFixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        var restClient = fixture.ServiceProvider.GetRequiredService<RestClient>();
        _alertRuleClient = new AlertRuleClient(restClient);
        _authClient = new AuthClient(restClient);
    }

    [Fact]
    public async Task GetAlertRules_ShouldReturnAlertRules_WhenUserIsAuthenticated()
    {
        // Arrange
        var loginResponse = await _authClient.LoginAsync(_testUser.Username, _testUser.Password);
        var accessToken = loginResponse.Data!.AccessToken;

        // Act
        var response = await _alertRuleClient.GetAlertRulesAsync(accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data.Should().AllSatisfy(alertRule => AlertRuleAssertion.AssertAllFieldsNotNull(alertRule));
    }

    [Fact]
    public async Task GetAlertRules_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var accessToken = "invalid_accessToken";

        // Act
        var response = await _alertRuleClient.GetAlertRulesAsync(accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateAlertRules_ShouldReturnAlertRule_WhenUserIsAuthenticated()
    {
        // Arrange
        var loginResponse = await _authClient.LoginAsync(_testUser.Username, _testUser.Password);
        var accessToken = loginResponse.Data!.AccessToken;

        var tickerSymbol = "TSLA";
        var condition = AlertCondition.PriceBelow;
        var targetValue = 100M;
        var triggerOnce = false;

        var alertRule = new AlertRuleRequest(tickerSymbol, condition, targetValue, triggerOnce);

        // Act
        var response = await _alertRuleClient.CreateAlertRuleAsync(accessToken, alertRule);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Data.Should().NotBeNull();
        try
        {
            response.Data.Id.Should().NotBe(Guid.Empty);
            response.Data.TickerSymbol.Should().Be(tickerSymbol);
            response.Data.Condition.Should().Be(condition);
            response.Data.TargetValue.Should().Be(targetValue);
            response.Data.IsActive.Should().BeTrue();
            response.Data.TriggerOnce.Should().Be(triggerOnce);
        }
        finally
        {
            // Clean up
            await _alertRuleClient.DeleteAlertRuleAsync(accessToken, response.Data.Id);
        }
    }

    [Fact]
    public async Task CreateAlertRules_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var accessToken = "invalid_token";

        var tickerSymbol = "TSLA";
        var condition = AlertCondition.PriceBelow;
        var targetValue = 100M;
        var triggerOnce = false;

        var alertRule = new AlertRuleRequest(tickerSymbol, condition, targetValue, triggerOnce);

        // Act
        var response = await _alertRuleClient.CreateAlertRuleAsync(accessToken, alertRule);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAlertRules_ShouldReturnOk_WhenUserIsAuthenticated()
    {
        // Arrange
        var loginResponse = await _authClient.LoginAsync(_testUser.Username, _testUser.Password);
        var accessToken = loginResponse.Data!.AccessToken;

        var tickerSymbol = "TSLA";
        var condition = AlertCondition.PriceBelow;
        var targetValue = 100M;
        var triggerOnce = false;

        var alertRule = new AlertRuleRequest(tickerSymbol, condition, targetValue, triggerOnce);

        var createResponse = await _alertRuleClient.CreateAlertRuleAsync(accessToken, alertRule);

        // Act
        var deleteResponse = await _alertRuleClient.DeleteAlertRuleAsync(accessToken, createResponse.Data!.Id);

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteAlertRules_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var accessToken = "invalid_accessToken";
        var alertRuleId = Guid.Parse("7f54756e-0a23-4751-a5c0-e754b688920c");

        // Act
        var deleteResponse = await _alertRuleClient.DeleteAlertRuleAsync(accessToken, alertRuleId);

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAlertRules_ShouldReturnNotFound_WhenAlertRuleIdIsInvalid()
    {
        // Arrange
        var loginResponse = await _authClient.LoginAsync(_testUser.Username, _testUser.Password);
        var accessToken = loginResponse.Data!.AccessToken;

        var alertRuleId = Guid.NewGuid();
        _output.WriteLine(alertRuleId.ToString());

        // Act
        var deleteResponse = await _alertRuleClient.DeleteAlertRuleAsync(accessToken, alertRuleId);

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ToggleAlertRule_ShouldReturnAlertRule_WhenUserIsAuthenticated()
    {
        // Arrange
        var loginResponse = await _authClient.LoginAsync(_testUser.Username, _testUser.Password);
        var accessToken = loginResponse.Data!.AccessToken;

        var tickerSymbol = "TSLA";
        var condition = AlertCondition.PriceBelow;
        var targetValue = 100M;
        var triggerOnce = false;

        var alertRule = new AlertRuleRequest(tickerSymbol, condition, targetValue, triggerOnce);

        var createResponse = await _alertRuleClient.CreateAlertRuleAsync(accessToken, alertRule);
        var id = createResponse.Data!.Id;
        var isActive = false;
        
        try
        {
            // Act
            var response = await _alertRuleClient.ToggleAlertRuleAsync(accessToken, id, isActive);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Data.Should().NotBeNull();
            response.Data.Id.Should().Be(id);
            response.Data.IsActive.Should().Be(isActive);
        }
        finally
        {
            // Clean up
            await _alertRuleClient.DeleteAlertRuleAsync(accessToken, createResponse.Data.Id);
        }
    }

    [Fact]
    public async Task ToggleAlertRule_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var accessToken = "invalid_accessToken";
        var id = Guid.Parse("7f54756e-0a23-4751-a5c0-e754b688920c");
        var isActive = false;

        // Act
        var response = await _alertRuleClient.ToggleAlertRuleAsync(accessToken, id, isActive);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ToggleAlertRule_ShouldReturnNotFound_WhenAlertRuleIsNotExist()
    {
        // Arrange
        var loginResponse = await _authClient.LoginAsync(_testUser.Username, _testUser.Password);
        var accessToken = loginResponse.Data!.AccessToken;

        var id = Guid.NewGuid();
        var isActive = false;

        // Act
        var response = await _alertRuleClient.ToggleAlertRuleAsync(accessToken, id, isActive);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
