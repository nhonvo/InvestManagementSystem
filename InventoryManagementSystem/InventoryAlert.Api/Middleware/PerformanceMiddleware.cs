using System.Diagnostics;
using System.Security.Claims;
using InventoryAlert.Domain.Configuration;

namespace InventoryAlert.Api.Middleware;

/// <summary>
/// Monitors and logs the execution time and status code of every HTTP request.
/// </summary>
public class PerformanceMiddleware(ILogger<PerformanceMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            var statusCode = context.Response.StatusCode;
            var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Anonymous";
            var correlationId = context.Items["X-Correlation-Id"]?.ToString() ?? "N/A";

            var level = statusCode >= 500 ? LogLevel.Error : (elapsedMs > 500 ? LogLevel.Warning : LogLevel.Information);
            
            using var scope = logger.BeginScope(new Dictionary<string, object>
            {
                ["UserId"] = userId,
                ["CorrelationId"] = correlationId,
                ["ElapsedMs"] = elapsedMs,
                ["StatusCode"] = statusCode
            });

            logger.Log(level, 
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs:F3}ms | User: {UserId} | CID: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                elapsedMs,
                userId,
                correlationId);
        }
    }
}
