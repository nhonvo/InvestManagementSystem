using System.Net;
using Dapper;
using FluentAssertions;
using InventoryAlert.IntegrationTests.Clients;
using InventoryAlert.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit.Abstractions;

namespace InventoryAlert.IntegrationTests.Tests.Jobs;

public class SyncPriceJobTest : IClassFixture<InjectionFixture>
{
    private readonly ITestOutputHelper _output;
    // private readonly InjectionFixture _fixture;
    private readonly ProductClient _productClient;

    public SyncPriceJobTest(InjectionFixture fixture, ITestOutputHelper output)
    {
        _productClient = fixture.ServiceProvider.GetRequiredService<ProductClient>();
        _output = output;
    }

    [Fact]
    public async Task SyncPriceJob_Should_Update_Product_Prices()
    {
        // Arrange
        
        // Act
        var request = await _productClient.TriggerPriceAlertAsync();
        request.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert
        // using var conn = new NpgsqlConnection(_fixture.ConnectionString);

        // var alert = await conn.QueryFirstOrDefaultAsync<dynamic>(
        //     "SELECT * FROM stock_alerts WHERE product_name = @Name",
        //     new { Name = "Test Product" });
    }
}
