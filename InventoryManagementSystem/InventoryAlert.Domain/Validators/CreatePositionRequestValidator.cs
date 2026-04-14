using FluentValidation;
using InventoryAlert.Domain.DTOs;

namespace InventoryAlert.Domain.Validators;

public class CreatePositionRequestValidator : AbstractValidator<CreatePositionRequest>
{
    public CreatePositionRequestValidator()
    {
        RuleFor(x => x.TickerSymbol)
            .NotEmpty()
            .MaximumLength(10)
            .Matches(@"^[A-Z0-9.]+$").WithMessage("TickerSymbol must be uppercase alphanumeric.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0);

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0)
            .LessThan(1_000_000);

        RuleFor(x => x.TradedAt)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .When(x => x.TradedAt.HasValue);
    }
}
