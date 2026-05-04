using System.Net;
using FluentAssertions;
using InventoryAlert.IntegrationTests.Abstractions;
using InventoryAlert.IntegrationTests.Clients;
using InventoryAlert.IntegrationTests.Fixtures;
using InventoryAlert.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using Xunit.Abstractions;

namespace InventoryAlert.IntegrationTests.Tests.Api;

[Trait("Category", "Api")]
public class StockApiTest : BaseIntegrationTest
{
    private readonly StockClient _stockClient;
    private readonly AuthClient _authClient;

    public StockApiTest(TestFixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _stockClient = new StockClient(Client);
        _authClient = new AuthClient(Client);
    }

    [Fact]
    
    public async Task GetStocks_ShouldReturnStocks_WhenTokenIsValid()
    {
        // Arrange
        var loginResponse = await _authClient.LoginAsync(_testUser.Username, _testUser.Password);
        var accessToken = loginResponse.Data!.AccessToken;

        // Act
        var response = await _stockClient.GetStocksAsync(accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
    }

    [Fact]
    
    public async Task SearchSymbol_ShouldReturnSymbols_WhenTokenIsValid()
    {
        // Arrange
        var loginResponse = await _authClient.LoginAsync(_testUser.Username, _testUser.Password);
        var accessToken = loginResponse.Data!.AccessToken;
        var q = "string";

        // Act
        var response = await _stockClient.SearchStockAsync(accessToken, q);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
    }

    [Fact]
    
    public async Task GetStockQuote_ShouldReturnStockQuote_WhenTokenIsValid()
    {
        // Arrange
        var loginResponse = await _authClient.LoginAsync(_testUser.Username, _testUser.Password);
        var accessToken = loginResponse.Data!.AccessToken;

        // Act
        var response = await _stockClient.GetStockQuoteAsync(accessToken, "AAPL");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
    }
}
