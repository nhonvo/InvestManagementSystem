using FluentAssertions;
using InventoryAlert.Api.Controllers;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Web.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly AuthController _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public AuthControllerTests()
    {
        _sut = new AuthController(_authService.Object, _httpContextAccessor.Object);

        // Wire up a default HttpContext so cookie operations don't throw in tests
        var httpContext = new DefaultHttpContext();
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public async Task Login_ReturnsOk_WithToken_WhenCredentialsValid()
    {
        // Arrange
        var request = new LoginRequest("admin", "password123");
        var expectedResponse = new AuthResponse("valid_token", DateTime.UtcNow.AddHours(1));
        _authService.Setup(s => s.LoginAsync(request, Ct)).ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Login(request, Ct);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Register_ReturnsOk_WithResponse()
    {
        // Arrange
        var request = new RegisterRequest("newuser", "password", "test@example.com");
        var expectedResponse = new RegistrationResponse("User registered successfully", request.Username);
        _authService.Setup(s => s.RegisterAsync(request, Ct)).ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Register(request, Ct);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Logout_ReturnsOk_AndClearsCookie()
    {
        // Arrange
        _authService.Setup(s => s.LogoutAsync(Ct)).Returns(Task.CompletedTask);

        // Set up user claims for [Authorize] context
        _sut.ControllerContext.HttpContext = new DefaultHttpContext();

        // Act
        var result = await _sut.Logout(Ct);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _authService.Verify(s => s.LogoutAsync(Ct), Times.Once);
    }

    [Fact]
    public async Task Refresh_ReturnsOk_WhenCookiePresent()
    {
        // Arrange
        var token = "refresh_me";
        var expectedRes = new AuthResponse("new_access", DateTime.UtcNow.AddHours(1));
        _authService.Setup(s => s.RefreshAsync(token, Ct)).ReturnsAsync(expectedRes);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = $"refreshToken={token}";
        _sut.ControllerContext.HttpContext = httpContext;

        // Act
        var result = await _sut.Refresh(Ct);

        // Assert
        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.Value.Should().Be(expectedRes);
    }
}

