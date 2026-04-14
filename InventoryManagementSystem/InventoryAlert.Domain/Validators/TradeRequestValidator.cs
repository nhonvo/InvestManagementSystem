using FluentValidation;
using InventoryAlert.Domain.DTOs;

namespace InventoryAlert.Domain.Validators;

public class TradeRequestValidator : AbstractValidator<TradeRequest>
{
    public TradeRequestValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.Quantity)
            .GreaterThan(0);

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .LessThan(1_000_000);

        RuleFor(x => x.Notes)
            .MaximumLength(500);
    }
}
