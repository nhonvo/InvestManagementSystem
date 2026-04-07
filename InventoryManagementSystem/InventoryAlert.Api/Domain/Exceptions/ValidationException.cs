using InventoryAlert.Api.Web.Models;

namespace InventoryAlert.Api.Domain.Exceptions;

public class ValidationException : Exception
{
    public ErrorResponse ErrorResponse { get; set; } = new();

    public ValidationException(string message) : base(message)
    {
    }

    public ValidationException(ErrorResponse errorResponse)
    {
        ErrorResponse = errorResponse;
    }
}
