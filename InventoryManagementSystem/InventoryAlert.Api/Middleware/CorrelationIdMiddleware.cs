namespace InventoryAlert.Api.Middleware;

/// <summary>
/// Attaches a unique CorrelationId to every request so logs from the Api
/// can be correlated with Worker job logs via the SQS message attributes.
/// The header precedence: incoming X-Correlation-Id → new Guid.
/// The Id is written back on the response header so clients can reference it.
/// </summary>
public sealed class CorrelationIdMiddleware : IMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Respect correlation ID sent by upstream (e.g. API Gateway, client)
        var correlationId = context.Request.Headers.TryGetValue(CorrelationIdHeader, out var existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString();

        // Push into logging context so all ILogger calls in this request will include it
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            context.Items[CorrelationIdHeader] = correlationId;
            context.Response.Headers[CorrelationIdHeader] = correlationId;

            await next(context);
        }
    }
}

