using System.Net;
using FluentAssertions;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.E2ETests.Abstractions;
using RestSharp;

namespace InventoryAlert.E2ETests;

public class AuthE2ETests : BaseE2ETest
{
    [Fact]
    public async Task Login_ShouldReturnToken_WhenCredentialsAreValid()
    {
        // Arrange
        var request = new RestRequest("api/v1/auth/login", Method.Post);
        request.AddJsonBody(new LoginRequest("admin", "password"));

        // Act
        var response = await Client.ExecuteAsync<AuthResponse>(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
    {
        // Arrange
        var request = new RestRequest("api/v1/auth/login", Method.Post);
        request.AddJsonBody(new LoginRequest("admin", "wrong_password"));

        // Act
        var response = await Client.ExecuteAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_ShouldReturnCreated_WhenValid()
    {
        // Arrange
        var request = new RestRequest("api/v1/auth/register", Method.Post);
        request.AddJsonBody(new RegisterRequest($"testuser_{Guid.NewGuid().ToString()[..8]}", "Test1234!", "testuser@example.com"));

        // Act
        var response = await Client.ExecuteAsync<RegistrationResponse>(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Username.Should().StartWith("testuser_");
    }

    [Fact]
    public async Task Refresh_ShouldFail_WithoutCookie()
    {
        // Arrange
        var request = new RestRequest("api/v1/auth/refresh", Method.Post);

        // Act
        var response = await Client.ExecuteAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ShouldReturnOk_WhenAuthenticated()
    {
        // Arrange
        await EnsureAuthenticatedAsync();
        var request = CreateAuthenticatedRequest("api/v1/auth/logout", Method.Post);

        // Act
        var response = await Client.ExecuteAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
