using System.Net;
using FluentAssertions;
using InventoryAlert.IntegrationTests.Abstractions;
using InventoryAlert.IntegrationTests.Clients;
using InventoryAlert.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using Xunit.Abstractions;

namespace InventoryAlert.IntegrationTests.Tests.Smoke;

public class AuthSmokeTest : BaseIntegrationTest
{
    private readonly AuthClient _client;
    
    public AuthSmokeTest(InjectionFixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _client = new AuthClient(fixture.ServiceProvider.GetRequiredService<RestClient>());
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task Login_ShouldReturnAccessToken()
    {
        // Arrange
        var username = _testUser.Username;
        var password = _testUser.Password;

        // Act
        var loginResponse = await _client.LoginAsync(username, password);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        loginResponse.Data.Should().NotBeNull();
        loginResponse.Data.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task Login_WithInvalidCredentials_ShouldNotCrash()
    {
        // Arrange
        var username = _testUser.Username;
        var password = "invalid_password";

        // Act
        var loginResponse = await _client.LoginAsync(username, password);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
