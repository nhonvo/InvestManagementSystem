using System.Net;
using InventoryAlert.Api.Models;
using InventoryAlert.Domain.Common.Constants;
using InventoryAlert.Domain.Common.Exceptions;

namespace InventoryAlert.Api.Middleware;

public class GlobalExceptionMiddleware(ILoggerFactory loggerFactory) : IMiddleware
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<GlobalExceptionMiddleware>();

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FAIL {Method} {Path} | {Message} | CID: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                ex.Message,
                context.Items["X-Correlation-Id"] ?? "N/A");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        var statusCode = HttpStatusCode.InternalServerError;
        var errorCode = $"{ApplicationConstants.Name}.{ErrorRespondCode.GENERAL_ERROR}";
        var errorMessage = "An error has occurred.";

        if (exception is UserFriendlyException userFriendlyException)
        {
            statusCode = userFriendlyException.ErrorCode switch
            {
                ErrorCode.NotFound => HttpStatusCode.NotFound,
                ErrorCode.VersionConflict or ErrorCode.ItemAlreadyExists or ErrorCode.Conflict => HttpStatusCode.Conflict,
                ErrorCode.BadRequest => HttpStatusCode.BadRequest,
                ErrorCode.Unauthorized => HttpStatusCode.Unauthorized,
                ErrorCode.Forbidden => HttpStatusCode.Forbidden,
                ErrorCode.Internal => HttpStatusCode.InternalServerError,
                ErrorCode.UnprocessableEntity => HttpStatusCode.UnprocessableEntity,
                _ => HttpStatusCode.InternalServerError
            };

            errorMessage = userFriendlyException.UserFriendlyMessage;
            errorCode = $"{ApplicationConstants.Name}.{userFriendlyException.ErrorCode.ToString().ToUpper()}";
        }
        else if (exception is KeyNotFoundException or NotFoundException)
        {
            statusCode = HttpStatusCode.NotFound;
            errorCode = $"{ApplicationConstants.Name}.{ErrorRespondCode.NOT_FOUND}";
            errorMessage = exception.Message;
        }
        else if (exception is FluentValidation.ValidationException or InventoryAlert.Domain.Common.Exceptions.ValidationException)
        {
            statusCode = HttpStatusCode.BadRequest;
            errorCode = $"{ApplicationConstants.Name}.{ErrorRespondCode.BAD_REQUEST}";
            errorMessage = exception.Message;
        }
        else if (exception is ArgumentException)
        {
            statusCode = HttpStatusCode.BadRequest;
            errorCode = $"{ApplicationConstants.Name}.{ErrorRespondCode.BAD_REQUEST}";
            errorMessage = exception.Message;
        }

        context.Response.StatusCode = (int)statusCode;
        var errorResponse = new ErrorResponse([new Error(errorCode, errorMessage)]);
        await context.Response.WriteAsync(errorResponse.ToString());
    }
}


