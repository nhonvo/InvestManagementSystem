using FluentValidation.TestHelper;
using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Validators;
using Xunit;

namespace InventoryAlert.UnitTests.Application.Validators;

public class ProductRequestValidatorTests
{
    private readonly ProductRequestValidator _validator = new();

    [Fact]
    public void ProductRequest_Valid_ReturnsSuccessful()
    {
        var model = new ProductRequest 
        { 
            Name = "Apple iPhone 15", 
            TickerSymbol = "AAPL", 
            Price = 999.99m, 
            StockCount = 50,
            PriceAlertThreshold = 0.05
        };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("", "AAPL", 100, 10)] // Empty Name
    [InlineData("string", "AAPL", 100, 10)] // Boilerplate Name
    [InlineData("Apple", "", 100, 10)] // Empty Ticker
    [InlineData("Apple", "too-long-ticker-symbol", 100, 10)] // Long Ticker
    [InlineData("Apple", "AAPL!", 100, 10)] // Invalid Chars Ticker
    [InlineData("Apple", "AAPL", 0, 10)] // Price 0
    [InlineData("Apple", "AAPL", 100, -1)] // Stock -1
    public void ProductRequest_InvalidData_ReturnsErrors(string name, string ticker, decimal price, int stock)
    {
        var model = new ProductRequest 
        { 
            Name = name, 
            TickerSymbol = ticker, 
            Price = price, 
            StockCount = stock 
        };
        var result = _validator.TestValidate(model);
        result.ShouldHaveAnyValidationError();
    }

    [Theory]
    [InlineData(0.005)] // Too low
    [InlineData(1.1)]   // Too high
    public void ProductRequest_InvalidThreshold_ReturnsError(double threshold)
    {
        var model = new ProductRequest 
        { 
            Name = "Apple", 
            TickerSymbol = "AAPL", 
            Price = 100, 
            StockCount = 10,
            PriceAlertThreshold = threshold
        };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.PriceAlertThreshold);
    }
}
