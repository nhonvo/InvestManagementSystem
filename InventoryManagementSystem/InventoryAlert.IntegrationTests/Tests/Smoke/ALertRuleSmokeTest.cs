using System.Net;
using FluentAssertions;
using InventoryAlert.IntegrationTests.Abstractions;
using InventoryAlert.IntegrationTests.Clients;
using InventoryAlert.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using Xunit.Abstractions;

namespace InventoryAlert.IntegrationTests.Tests.Smoke;

public class ALertRuleSmokeTest : BaseIntegrationTest
{
    private readonly AlertRuleClient _alertRuleClient;
    private readonly AuthClient _authClient;

    public ALertRuleSmokeTest(InjectionFixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        var restClient = fixture.ServiceProvider.GetRequiredService<RestClient>();
        _alertRuleClient = new AlertRuleClient(restClient);
        _authClient = new AuthClient(restClient);
    }

    [Fact]
    [Trait("Category", "Smoke")]
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
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task GetAlertRules_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var accessToken = "invalid_accessToken";

        // Act
        var response = await _alertRuleClient.GetAlertRulesAsync(accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
