using System.Net;
using FluentAssertions;
using InventoryAlert.IntegrationTests.Abstractions;
using InventoryAlert.IntegrationTests.Clients;
using InventoryAlert.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using Xunit.Abstractions;

namespace InventoryAlert.IntegrationTests.Tests.Api;

public class StockApiTest : BaseIntegrationTest
{
    private readonly StockClient _stockClient;
    private readonly AuthClient _authClient;

    public StockApiTest(InjectionFixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _stockClient = new StockClient(fixture.ServiceProvider.GetRequiredService<RestClient>());
        _authClient = new AuthClient(fixture.ServiceProvider.GetRequiredService<RestClient>());
    }

    // [Fact]
    // public async Task GetStocks_ShouldReturnStocks_WhenTokenIsValid()
    // {
    //     // Arrange
    //     var loginResponse = await _authClient.LoginAsync(_testUser.Username, _testUser.Password);
    //     var accessToken = loginResponse.Data!.AccessToken;

    //     // Act
    //     var response = await _stockClient.GetStocksAsync(accessToken);

    //     // Assert
    //     response.StatusCode.Should().Be(HttpStatusCode.OK);
    //     response.Data.Should().NotBeNull();
    // }

    [Fact]
    public async Task GetStockQuote_ShouldReturnStockQuote_WhenTokenIsValid()
    {
        // Arrange
        var loginResponse = await _authClient.LoginAsync(_testUser.Username, _testUser.Password);
        var accessToken = loginResponse.Data!.AccessToken;

        // Act
        var response = await _stockClient.GetStockQuoteAsync(accessToken, "AAPL");
        _output.WriteLine($"Response: {response.Content}"); // Log the response content for debugging
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
    }
}
