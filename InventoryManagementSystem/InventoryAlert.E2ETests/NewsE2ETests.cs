using System.Net;
using FluentAssertions;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.E2ETests.Abstractions;
using RestSharp;

namespace InventoryAlert.E2ETests;

public class NewsE2ETests : BaseE2ETest
{
    [Fact]
    public async Task GetMarketNews_ShouldReturnOk()
    {
        // 1. Arrange
        await EnsureAuthenticatedAsync();

        // 2. Act
        var request = CreateAuthenticatedRequest("api/v1/news/market?category=general&page=1", Method.Get);
        var response = await Client.ExecuteAsync<IEnumerable<NewsResponse>>(request);

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCompanyNews_ShouldReturnOk()
    {
        // 1. Arrange
        await EnsureAuthenticatedAsync();

        // 2. Act
        var request = CreateAuthenticatedRequest("api/v1/news/company/AAPL", Method.Get);
        var response = await Client.ExecuteAsync<IEnumerable<NewsResponse>>(request);

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCompanyNews_WithDates_ShouldReturnOk()
    {
        // 1. Arrange
        await EnsureAuthenticatedAsync();
        const string symbol = "AAPL";
        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var to = DateOnly.FromDateTime(DateTime.UtcNow);

        // 2. Act
        var request = CreateAuthenticatedRequest($"api/v1/news/company/{symbol}", Method.Get);
        request.AddQueryParameter("from", from.ToString("yyyy-MM-dd"));
        request.AddQueryParameter("to", to.ToString("yyyy-MM-dd"));

        var response = await Client.ExecuteAsync<IEnumerable<NewsResponse>>(request);

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
    }
}
