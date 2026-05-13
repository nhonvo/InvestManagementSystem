namespace InventoryAlert.Api.Extensions;

public static class HttpContextExtensions
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public static string GetCorrelationId(this HttpContext context)
    {
        return context.Items[CorrelationIdHeader]?.ToString() ?? "N/A";
    }
}
