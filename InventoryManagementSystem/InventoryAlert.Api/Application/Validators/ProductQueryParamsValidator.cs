using FluentValidation;
using InventoryAlert.Api.Application.DTOs;

namespace InventoryAlert.Api.Application.Validators;

public class ProductQueryParamsValidator : AbstractValidator<ProductQueryParams>
{
    public ProductQueryParamsValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be 1 or greater (default is 1).");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50)
            .WithMessage("PageSize must be between 1 and 50 items per page.");
            
        RuleFor(x => x.MinStock)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinStock.HasValue)
            .WithMessage("MinStock cannot be a negative value.");

        RuleFor(x => x.MaxStock)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MaxStock.HasValue)
            .WithMessage("MaxStock cannot be a negative value.");

        RuleFor(x => x.MaxStock)
            .GreaterThanOrEqualTo(x => x.MinStock!.Value)
            .When(x => x.MinStock.HasValue && x.MaxStock.HasValue)
            .WithMessage("The Maximum Stock filter cannot be lower than the Minimum Stock filter.");
            
        RuleFor(x => x.SortBy)
            .Must(v => string.IsNullOrEmpty(v) || new[] { "name_asc", "name_desc", "price_asc", "price_desc", "stock_asc", "stock_desc" }.Contains(v.ToLowerInvariant()))
            .WithMessage("Invalid 'SortBy' value. Supported: name_asc, name_desc, price_asc, price_desc, stock_asc, stock_desc.");
    }
}
