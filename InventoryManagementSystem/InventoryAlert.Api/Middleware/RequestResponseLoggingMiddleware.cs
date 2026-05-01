using System.Diagnostics;
using System.Text;
using InventoryAlert.Domain.Configuration;

namespace InventoryAlert.Api.Middleware;

public class RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger, AppSettings settings)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger = logger;
    private readonly bool _enableBodyLogging = settings.Api?.EnableBodyLogging ?? false;
    private const int MaxBodyLength = 4096; // 4KB limit to prevent massive logs

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_enableBodyLogging || !context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        // 1. Log Request
        context.Request.EnableBuffering();
        var requestBody = await ReadStreamAsync(context.Request.Body);
        var correlationId = context.Items["X-Correlation-Id"]?.ToString() ?? "N/A";

        _logger.LogInformation("HTTP Request: {Method} {Path} | CID: {CorrelationId} | Body: {Body}",
            context.Request.Method,
            context.Request.Path,
            correlationId,
            requestBody);

        context.Request.Body.Position = 0;

        // 2. Log Response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            context.Response.Body.Position = 0;
            var responseBodyText = await ReadStreamAsync(context.Response.Body);
            context.Response.Body.Position = 0;
            
            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;

            _logger.LogInformation("HTTP Response: {Method} {Path} responded {StatusCode} in {ElapsedMs}ms | CID: {CorrelationId} | Body: {Body}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId,
                responseBodyText);
        }
    }

    private static async Task<string> ReadStreamAsync(Stream stream)
    {
        if (stream.Length == 0) return string.Empty;

        using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true);
        var body = await reader.ReadToEndAsync();
        
        // Truncate and redact sensitive data (simplified)
        if (body.Length > MaxBodyLength)
        {
            body = body[..MaxBodyLength] + "... [TRUNCATED]";
        }
        
        if (body.Contains("password", StringComparison.OrdinalIgnoreCase))
        {
             return "[REDACTED - Contains Sensitive Info]";
        }

        return body;
    }
}
