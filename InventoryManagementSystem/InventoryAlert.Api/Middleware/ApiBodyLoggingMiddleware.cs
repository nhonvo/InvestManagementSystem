using System.Text;
using System.Text.Json;
using InventoryAlert.Domain.Configuration;

namespace InventoryAlert.Api.Middleware;

/// <summary>
/// Captures and logs the JSON Request and Response bodies for API endpoints.
/// </summary>
public class ApiBodyLoggingMiddleware(ILogger<ApiBodyLoggingMiddleware> logger, AppSettings settings) : IMiddleware
{
    private readonly bool _enableBodyLogging = settings.Api?.EnableBodyLogging ?? false;
    private const int MaxBodyLength = 4096;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var isApi = context.Request.Path.StartsWithSegments("/api");

        if (!_enableBodyLogging || !isApi)
        {
            await next(context);
            return;
        }

        // 1. Capture Request Body
        context.Request.EnableBuffering();
        var requestBodyText = await ReadStreamAsync(context.Request.Body);
        context.Request.Body.Position = 0;

        // 2. Wrap Response Body Stream
        var originalBodyStream = context.Response.Body;
        using var responseBodyWrapper = new MemoryStream();
        context.Response.Body = responseBodyWrapper;

        try
        {
            await next(context);
        }
        finally
        {
            // 3. Read Response Body
            responseBodyWrapper.Position = 0;
            var responseBodyText = await ReadStreamAsync(responseBodyWrapper);
            
            // 4. Restore original stream so client receives data
            responseBodyWrapper.Position = 0;
            await responseBodyWrapper.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;

            // 5. Log structured JSON
            var correlationId = context.Items["X-Correlation-Id"]?.ToString() ?? "N/A";
            
            using var scope = logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["RequestBody"] = TryParseJson(requestBodyText),
                ["ResponseBody"] = TryParseJson(responseBodyText)
            });

            logger.LogInformation("API Payload | CID: {CorrelationId} | Method: {Method} | Path: {Path}", 
                correlationId, context.Request.Method, context.Request.Path);
        }
    }

    private static object? TryParseJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        try
        {
            return JsonSerializer.Deserialize<object>(text);
        }
        catch
        {
            return text;
        }
    }

    private static async Task<string> ReadStreamAsync(Stream stream)
    {
        if (stream.Length == 0) return string.Empty;
        using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true);
        var body = await reader.ReadToEndAsync();
        
        if (body.Length > MaxBodyLength) body = body[..MaxBodyLength] + "... [TRUNCATED]";
        if (body.Contains("password", StringComparison.OrdinalIgnoreCase)) return "[REDACTED]";

        return body;
    }
}
