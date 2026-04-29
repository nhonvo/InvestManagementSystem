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
            var correlationId = context.Items["X-Correlation-Id"]?.ToString() ?? "N/A";
            _logger.LogError(ex, "An unhandled exception has occurred while executing the request. | CID: {CorrelationId}", correlationId);
            await HandleExceptionAsync(context, ex, correlationId);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
    {
        context.Response.ContentType = "application/json";
        
        var (statusCode, respondCode) = exception switch
        {
            UserFriendlyException ex => (MapStatusCode(ex.ErrorCode), MapErrorCode(ex.ErrorCode)),
            KeyNotFoundException or NotFoundException => (HttpStatusCode.NotFound, ErrorRespondCode.NOT_FOUND),
            FluentValidation.ValidationException or InventoryAlert.Domain.Common.Exceptions.ValidationException => (HttpStatusCode.BadRequest, ErrorRespondCode.BAD_REQUEST),
            ArgumentException => (HttpStatusCode.BadRequest, ErrorRespondCode.BAD_REQUEST),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, ErrorRespondCode.UNAUTHORIZED),
            _ => (HttpStatusCode.InternalServerError, ErrorRespondCode.GENERAL_ERROR)
        };

        var errorCode = $"{ApplicationConstants.Name}.{respondCode}";
        var errorMessage = exception switch
        {
            UserFriendlyException ufe => ufe.UserFriendlyMessage,
            KeyNotFoundException or NotFoundException or FluentValidation.ValidationException or InventoryAlert.Domain.Common.Exceptions.ValidationException or ArgumentException or UnauthorizedAccessException => exception.Message,
            _ => "An error has occurred."
        };

        context.Response.StatusCode = (int)statusCode;
        
        var errorResponse = new ErrorResponse(
            new Error(errorCode, errorMessage),
            correlationId
        );

        await context.Response.WriteAsync(errorResponse.ToString());
    }

    private static HttpStatusCode MapStatusCode(ErrorCode errorCode) => errorCode switch
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

    private static ErrorRespondCode MapErrorCode(ErrorCode errorCode) => errorCode switch
    {
        ErrorCode.NotFound => ErrorRespondCode.NOT_FOUND,
        ErrorCode.VersionConflict => ErrorRespondCode.VERSION_CONFLICT,
        ErrorCode.ItemAlreadyExists => ErrorRespondCode.ITEM_ALREADY_EXISTS,
        ErrorCode.Conflict => ErrorRespondCode.CONFLICT,
        ErrorCode.BadRequest => ErrorRespondCode.BAD_REQUEST,
        ErrorCode.Unauthorized => ErrorRespondCode.UNAUTHORIZED,
        ErrorCode.Forbidden => ErrorRespondCode.FORBIDDEN,
        ErrorCode.Internal => ErrorRespondCode.INTERNAL_ERROR,
        ErrorCode.UnprocessableEntity => ErrorRespondCode.UNPROCESSABLE_ENTITY,
        _ => ErrorRespondCode.GENERAL_ERROR
    };
}
