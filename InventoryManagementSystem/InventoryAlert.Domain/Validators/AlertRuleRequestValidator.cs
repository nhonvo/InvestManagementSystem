using FluentValidation;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;

namespace InventoryAlert.Domain.Validators;

public class AlertRuleRequestValidator : AbstractValidator<AlertRuleRequest>
{
    public AlertRuleRequestValidator()
    {
        RuleFor(x => x.TickerSymbol)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.Condition)
            .IsInEnum();

        RuleFor(x => x.TargetValue)
            .GreaterThan(0);

        // Condition-specific rules
        RuleFor(x => x.TargetValue)
            .InclusiveBetween(0.01m, 100.00m)
            .When(x => x.Condition == AlertCondition.PercentDropFromCost)
            .WithMessage("PercentDropFromCost must be between 0.01 and 100.");

        RuleFor(x => x.TargetValue)
            .Must(x => x % 1 == 0)
            .When(x => x.Condition == AlertCondition.LowHoldingsCount)
            .WithMessage("LowHoldingsCount target must be a whole number.");
    }
}
