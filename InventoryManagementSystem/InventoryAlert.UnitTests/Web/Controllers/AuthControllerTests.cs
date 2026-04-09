using FluentAssertions;
using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Web.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IMemoryCache> _cache = new();
    private readonly AuthController _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public AuthControllerTests()
    {
        _sut = new AuthController(_authService.Object);
    }

    [Fact]
    public async Task Login_ReturnsOk_WithToken_WhenCredentialsValid()
    {
        // Arrange
        var request = new LoginRequest("admin", "password123");
        var expectedResponse = new AuthResponse("valid_token");
        _authService.Setup(s => s.LoginAsync(request, Ct))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Login(request, _cache.Object, Ct);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Register_ReturnsOk_WithResponse()
    {
        // Arrange
        var request = new RegisterRequest("newuser", "password", "test@example.com");
        var expectedResponse = new RegistrationResponse("User registered successfully");
        _authService.Setup(s => s.RegisterAsync(request, Ct))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Register(request, Ct);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expectedResponse);
    }
}
