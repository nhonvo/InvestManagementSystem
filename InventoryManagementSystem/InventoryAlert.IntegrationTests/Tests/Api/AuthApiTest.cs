using System.Net;
using FluentAssertions;
using InventoryAlert.IntegrationTests.Abstractions;
using InventoryAlert.IntegrationTests.Clients;
using InventoryAlert.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using Xunit.Abstractions;

namespace InventoryAlert.IntegrationTests.Tests.Api;

public class AuthApiTest : BaseIntegrationTest
{
    private readonly AuthClient _client;

    public AuthApiTest(InjectionFixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _client = new AuthClient(fixture.ServiceProvider.GetRequiredService<RestClient>());
    }

    [Fact]
    public async Task Login_ShouldReturnAccessToken_WhenCredentialsAreValid()
    {
        // Arrange

        // Act
        var loginResponse = await _client.LoginAsync(_testUser.Username, _testUser.Password);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        loginResponse.Data.Should().NotBeNull();
        loginResponse.Data.AccessToken.Should().NotBeNullOrEmpty();
        _output.WriteLine($"Access Token: {loginResponse.Data.AccessToken}");
        _output.WriteLine($"Cookies: {string.Join(", ", loginResponse.Cookies!.Select(c => $"{c.Name}={c.Value}"))}");
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenPasswordIsInvalid()
    {
        // Arrange

        // Act
        var loginResponse = await _client.LoginAsync(_testUser.Username, "invalid");

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenUsernameIsInvalid()
    {
        // Arrange

        // Act
        var loginResponse = await _client.LoginAsync("invalid", _testUser.Password);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // [Fact]
    // public async Task RefreshToken_ShouldReturnNewAccessToken_WhenRefreshTokenIsValid()
    // {
    //     // Arrange
    //     var loginResponse = await _client.LoginAsync("admin", "password");
    //     var refreshToken = loginResponse.Data!.AccessToken;

    //     // Act
    //     var refreshResponse = await _client.RefreshTokenAsync(refreshToken);

    //     // Assert
    //     refreshResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    //     refreshResponse.Data.Should().NotBeNull();
    //     refreshResponse.Data.AccessToken.Should().NotBeNullOrEmpty();
    // }

    [Fact]
    public async Task Logout_ShouldInvalidateTokens_WhenCalledWithValidSession()
    {
        // Arrange
        var loginResponse = await _client.LoginAsync(_testUser.Username, _testUser.Password);
        var refreshToken = loginResponse.Data!.AccessToken;

        // Act
        var logoutResponse = await _client.LogoutAsync(refreshToken);

        // Assert
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
