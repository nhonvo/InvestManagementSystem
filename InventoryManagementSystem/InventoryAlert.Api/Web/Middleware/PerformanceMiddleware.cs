using System.Diagnostics;

namespace InventoryAlert.Api.Web.Middleware;

public class PerformanceMiddleware(ILoggerFactory loggerFactory) : IMiddleware
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<PerformanceMiddleware>();

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();

        await next(context);

        stopwatch.Stop();
        var timeTaken = stopwatch.Elapsed;

        if (timeTaken.TotalMilliseconds > 500) // Log as warning if slow
        {
            _logger.LogWarning("Execution time: {timeTaken} for {Path}",
                timeTaken.ToString(@"m\:ss\.fff"),
                context.Request.Path);
        }
        else
        {
            _logger.LogInformation("Execution time: {timeTaken}", timeTaken.ToString(@"m\:ss\.fff"));
        }
    }
}
