using FluentValidation;

namespace InventoryAlert.Api.Application.Validators;

/// <summary>
/// FluentValidation rules for creating/updating a Product.
/// Registered in DI and auto-invoked by the ASP.NET model-binding pipeline.
/// </summary>
public sealed class ProductRequestValidator : AbstractValidator<DTOs.ProductRequest>
{
    public ProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(256).WithMessage("Name must not exceed 256 characters.")
            .Must(NotBeBoilerplate).WithMessage("Name cannot be a boilerplate placeholder like 'string'.");

        RuleFor(x => x.TickerSymbol)
            .NotEmpty().WithMessage("TickerSymbol is required.")
            .MaximumLength(16).WithMessage("TickerSymbol must not exceed 16 characters.")
            .Matches(@"^[A-Z0-9.]{1,16}$").WithMessage("TickerSymbol must be 1–16 uppercase letters, numbers, or dots (e.g. AAPL, BRK.A).");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.");

        RuleFor(x => x.StockCount)
            .GreaterThanOrEqualTo(0).WithMessage("StockCount must be 0 or greater.");

        RuleFor(x => x.StockAlertThreshold)
            .GreaterThanOrEqualTo(0)
            .When(x => x.StockAlertThreshold.HasValue)
            .WithMessage("StockAlertThreshold must be 0 or greater if provided.");

        RuleFor(x => x.PriceAlertThreshold)
            .InclusiveBetween(0.01, 1.0)
            .When(x => x.PriceAlertThreshold.HasValue)
            .WithMessage("PriceAlertThreshold must be between 0.01 and 1.0 (1% to 100%) if provided.");
    }

    private static bool NotBeBoilerplate(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return true;
        var lower = name.ToLower();
        return lower != "string" && lower != "placeholder" && lower != "test";
    }
}
