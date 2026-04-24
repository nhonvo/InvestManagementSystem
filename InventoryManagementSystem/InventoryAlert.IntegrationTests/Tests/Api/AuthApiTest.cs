using System.Net;
using FluentAssertions;
using InventoryAlert.IntegrationTests.Abstractions;
using InventoryAlert.IntegrationTests.Clients;
using InventoryAlert.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;
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
        var username = _testUser.Username;
        var password = _testUser.Password;

        // Act
        var loginResponse = await _client.LoginAsync(username, password);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        loginResponse.Data.Should().NotBeNull();
        loginResponse.Data.AccessToken.Should().NotBeNullOrEmpty();
        _output.WriteLine($"Access Token: {loginResponse.Data.AccessToken}");
        _output.WriteLine(loginResponse.Cookies[0].Value);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenPasswordIsInvalid()
    {
        // Arrange
        var username = _testUser.Username;
        var password = "invalid_password";

        // Act
        var loginResponse = await _client.LoginAsync(username, password);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenUsernameIsInvalid()
    {
        // Arrange
        var username = "invalid_username";
        var password = _testUser.Password;

        // Act
        var loginResponse = await _client.LoginAsync(username, password);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnNewAccessToken_WhenRefreshTokenIsValid()
    {
        // Arrange
        var options = new RestClientOptions(_fixture.ServiceProvider.GetRequiredService<InventoryAlert.IntegrationTests.Config.AppSettings>().ApiSettings.BaseUrl)
        {
            CookieContainer = new System.Net.CookieContainer()
        };
        var client = new AuthClient(new RestClient(options, configureSerialization: s => s.UseNewtonsoftJson()));
        
        var username = _testUser.Username;
        var password = _testUser.Password;

        var loginResponse = await client.LoginAsync(username, password);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshTokenCookie = loginResponse.Cookies.FirstOrDefault(c => c.Name == "refreshToken");
        refreshTokenCookie.Should().NotBeNull();

        // Act
        var refreshResponse = await client.RefreshTokenAsync(refreshTokenCookie.Value);

        // Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        refreshResponse.Data.Should().NotBeNull();
        refreshResponse.Data.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnUnauthorized_WhenRefreshTokenIsInvalid()
    {
        // Arrange
        var accessToken = "invalid_token";

        // Act
        var refreshResponse = await _client.RefreshTokenAsync(accessToken);

        // Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ShouldInvalidateTokens_WhenCalledWithValidToken()
    {
        // Arrange
        var username = _testUser.Username;
        var password = _testUser.Password;

        var loginResponse = await _client.LoginAsync(username, password);
        var refreshToken = loginResponse.Data!.AccessToken;

        // Act
        var logoutResponse = await _client.LogoutAsync(refreshToken);

        // Assert
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_ShouldReturnUnauthorized_WhenCalledWithInvalidToken()
    {
        // Arrange
        var refreshToken = "invalid_token";

        // Act
        var logoutResponse = await _client.LogoutAsync(refreshToken);

        // Assert
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
