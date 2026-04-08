using FluentValidation;
using FluentValidation.AspNetCore;
using FluentValidation.Results;
using InventoryAlert.Contracts.Common.Constants;
using InventoryAlert.Api.Web.Models;
using Microsoft.AspNetCore.Mvc;
using ValidationException = InventoryAlert.Contracts.Common.Exceptions.ValidationException;

namespace InventoryAlert.Api.Web.Validations;

public class ValidatorInterceptor : IValidatorInterceptor
{
    public IValidationContext BeforeAspNetValidation(ActionContext actionContext, IValidationContext commonContext)
    {
        return commonContext;
    }

    public ValidationResult AfterAspNetValidation(ActionContext actionContext, IValidationContext Edit, ValidationResult result)
    {
        if (!result.IsValid)
        {
            var errors = result.Errors.Select(e => new Error(
                $"{ApplicationConstants.Name}.{ErrorRespondCode.BAD_REQUEST}", e.ErrorMessage)
            {
                Property = e.PropertyName
            });

            var errorResponse = new ErrorResponse(errors);
            var exception = new ValidationException(ApplicationConstants.Messages.ValidationFailed);

            actionContext.ModelState.AddModelError(ApplicationConstants.FluentValidationErrorKey, exception.Message);
            // Attach the exception to the model state so the ValidateModelFilter can catch it.
            actionContext.ModelState[ApplicationConstants.FluentValidationErrorKey]!.Errors.Clear();
            actionContext.ModelState[ApplicationConstants.FluentValidationErrorKey]!.Errors.Add(exception);
        }
        return result;
    }
}
