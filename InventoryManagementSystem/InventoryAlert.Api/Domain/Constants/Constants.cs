namespace InventoryAlert.Api.Domain.Constants;

public static class ApplicationConstants
{
    public const string Name = "InventoryAlert";
    public const string FluentValidationErrorKey = "FluentValidation";

    public static class Messages
    {
        public const string ProductNotFound = "Product with id {0} was not found.";
        public const string InvalidCredentials = "Invalid credentials.";
        public const string AuthConfigMissing = "Authentication is not safely configured.";
        public const string JwtKeyMissing = "JWT Key is missing.";
        public const string ValidationFailed = "Input validation failed. Please check your data.";
        public const string TransactionFailed = "A database transaction failed. Please try again later.";
    }

    public static class HttpClientNames
    {
        public const string Finnhub = "Finnhub";
        public const string Telegram = "Telegram";
    }
}

public enum ErrorRespondCode
{
    NOT_FOUND,
    VERSION_CONFLICT,
    ITEM_ALREADY_EXISTS,
    CONFLICT,
    BAD_REQUEST,
    UNAUTHORIZED,
    INTERNAL_ERROR,
    UNPROCESSABLE_ENTITY,
    GENERAL_ERROR
}

public static class HealthCheck
{
    public const string DBHealthCheck = "PostgreSQL";
    public const string InfrastructureCheck = "Infrastructure";
    public const string ExternalServiceCheck = "ExternalService";
}
