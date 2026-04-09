using InventoryAlert.Api.Web.Models;
using InventoryAlert.Contracts.Common.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InventoryAlert.Api.Web.Filters;

public class ValidateModelFilter : IActionFilter
{
    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No implementation needed for this filter.
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(ms => ms.Value != null && ms.Value.Errors.Any())
                .SelectMany(ms =>
                {
                    var key = ms.Key == ApplicationConstants.FluentValidationErrorKey || ms.Key == string.Empty
                        ? "General"
                        : ms.Key;

                    return ms.Value!.Errors.Select(error => new { Key = key, error.ErrorMessage });
                })
                .Select(errorDetail => new Error(
                    $"{ApplicationConstants.Name}.{ErrorRespondCode.BAD_REQUEST}",
                    errorDetail.ErrorMessage)
                {
                    Property = errorDetail.Key
                })
                .ToList();

            context.Result = new BadRequestObjectResult(new ErrorResponse(errors));
        }
    }
}
