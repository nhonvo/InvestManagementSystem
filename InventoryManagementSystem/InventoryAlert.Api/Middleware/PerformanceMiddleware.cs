using System.Diagnostics;

namespace InventoryAlert.Api.Middleware;

public class PerformanceMiddleware(ILoggerFactory loggerFactory) : IMiddleware
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<PerformanceMiddleware>();

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();

        await next(context);

        stopwatch.Stop();
        var timeTaken = stopwatch.Elapsed;

        if (timeTaken.TotalMilliseconds > 500)
        {
            _logger.LogWarning("SLOW REQ: {Method} {Path} responded {StatusCode} in {TimeTaken}ms | CID: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                timeTaken.TotalMilliseconds.ToString("F3"),
                context.Items["X-Correlation-Id"] ?? "N/A");
        }
        else
        {
            _logger.LogInformation("{Method} {Path} responded {StatusCode} in {TimeTaken}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                timeTaken.TotalMilliseconds.ToString("F3"));
        }
    }
}

