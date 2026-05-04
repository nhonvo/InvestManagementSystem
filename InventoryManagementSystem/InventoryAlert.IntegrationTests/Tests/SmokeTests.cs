using FluentAssertions;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InventoryAlert.IntegrationTests.Tests;

[Collection("IntegrationTests")]
public class SmokeTests(TestFixture fixture) : IAsyncLifetime
{
    public virtual async Task InitializeAsync()
    {
        await fixture.ResetStateAsync();
    }

    public virtual Task DisposeAsync() => Task.CompletedTask;
    [Fact]
    [Trait("Category", "Smoke")]
    public async Task WireMock_Ping_ReturnsPong()
    {
        // Arrange
        using var client = new HttpClient();
        var wireMockUrl = fixture.WireMock.Urls[0];
        
        fixture.WireMock.Given(WireMock.RequestBuilders.Request.Create().WithPath("/ping").UsingGet())
            .RespondWith(WireMock.ResponseBuilders.Response.Create().WithStatusCode(200).WithBody("pong"));

        // Act
        var response = await client.GetAsync($"{wireMockUrl}/ping");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("pong");
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void DI_CanResolveServiceAndCaptureLog()
    {
        // Arrange
        var service = fixture.ServiceProvider.GetRequiredService<IStockDataService>();
        var logger = fixture.ServiceProvider.GetRequiredService<ILogger<SmokeTests>>();

        // Act
        logger.LogInformation("Smoke test log entry");

        // Assert
        service.Should().NotBeNull();
        fixture.LoggerProvider.Entries.Should().Contain(e => e.Message == "Smoke test log entry");
    }
}
