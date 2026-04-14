using FluentValidation.TestHelper;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Validators;
using Xunit;

namespace InventoryAlert.UnitTests.Application.Validations;

public class AuthValidatorsTests
{
    private readonly LoginRequestValidator _loginValidator = new();
    private readonly RegisterRequestValidator _registerValidator = new();

    [Fact]
    public void LoginRequest_Valid_ReturnsSuccessful()
    {
        var model = new LoginRequest("user", "password");
        var result = _loginValidator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void LoginRequest_EmptyFields_ReturnsErrors()
    {
        var model = new LoginRequest("", "");
        var result = _loginValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Username);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void RegisterRequest_Valid_ReturnsSuccessful()
    {
        var model = new RegisterRequest("user", "password", "test@example.com");
        var result = _registerValidator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("", "test@example.com", "123456")] // Empty Username
    [InlineData("us", "test@example.com", "123456")] // Short Username
    [InlineData("user", "", "123456")] // Empty Email
    [InlineData("user", "invalid-email", "123456")] // Invalid Email
    [InlineData("user", "test@example.com", "")] // Empty Password
    [InlineData("user", "test@example.com", "123")] // Short Password
    public void RegisterRequest_InvalidData_ReturnsErrors(string username, string email, string password)
    {
        var model = new RegisterRequest(username, password, email);
        var result = _registerValidator.TestValidate(model);
        Assert.False(result.IsValid);
    }
}

