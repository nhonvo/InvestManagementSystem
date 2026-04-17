using System.Net;
using FluentAssertions;
using InventoryAlert.IntegrationTests.Clients;
using InventoryAlert.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace InventoryAlert.IntegrationTests.Tests.Api;

public class CheckWiremock
{
    private readonly MockFinnhubClient _client;
    private readonly ITestOutputHelper _output;

    public CheckWiremock(InjectionFixture fixture, ITestOutputHelper output)
    {
        _client = fixture.ServiceProvider.GetRequiredService<MockFinnhubClient>();
        _output = output;
    }

    [Fact]
    public async Task CheckWiremock_ShouldReturnSuccess()
    {
        // Act
        var response = await _client.CheckWiremockAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _output.WriteLine($"Response: {response.Content}");
    }
}
