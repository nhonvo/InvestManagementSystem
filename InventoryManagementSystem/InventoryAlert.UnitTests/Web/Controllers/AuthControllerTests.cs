using FluentAssertions;
using InventoryAlert.Api.Application.Common.Exceptions;
using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Web.Configuration;
using InventoryAlert.Api.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace InventoryAlert.UnitTests.Web.Controllers;

public class AuthControllerTests
{
    private readonly AppSettings _settings;
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        _settings = new AppSettings
        {
            Auth = new AuthSettings { Username = "admin", Password = "password123" },
            Jwt = new JwtSettings { Key = "super_secret_key_that_is_long_enough_for_hmac_sha256", Issuer = "issuer", Audience = "audience" }
        };
        _sut = new AuthController(_settings);
    }

    [Fact]
    public void Login_ReturnsOk_WithToken_WhenCredentialsValid()
    {
        // Arrange
        var request = new LoginRequest { Username = "admin", Password = "password123" };

        // Act
        var result = _sut.Login(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value!.ToString().Should().Contain("token");
    }

    [Fact]
    public void Login_ThrowsUnauthorized_WhenCredentialsInvalid()
    {
        // Arrange
        var request = new LoginRequest { Username = "admin", Password = "wrong" };

        // Act
        var act = () => _sut.Login(request);

        // Assert
        act.Should().Throw<UserFriendlyException>()
           .And.ErrorCode.Should().Be(ErrorCode.Unauthorized);
    }

    [Fact]
    public void Register_ReturnsOk_Always()
    {
        // Arrange
        var request = new RegisterRequest { Username = "newuser", Password = "password" };

        // Act
        var result = _sut.Register(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}
