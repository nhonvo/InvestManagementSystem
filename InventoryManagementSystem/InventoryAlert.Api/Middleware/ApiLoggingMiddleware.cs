using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using InventoryAlert.Api.Extensions;
using InventoryAlert.Domain.Configuration;

namespace InventoryAlert.Api.Middleware;

/// <summary>
/// Unified logging middleware that captures:
/// 1. Request &amp; Response bodies (if JSON)
/// 2. Execution time (ElapsedMs)
/// 3. User &amp; Correlation context
/// All in a single structured Seq event.
/// </summary>
public class ApiLoggingMiddleware(ILogger<ApiLoggingMiddleware> logger, AppSettings settings) : IMiddleware
{
    private readonly bool _enableBodyLogging = settings.Api?.EnableBodyLogging ?? true;
    private const int MaxBodyLength = 4096;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        var isApi = context.Request.Path.StartsWithSegments("/api");

        // If not API or logging disabled, just capture performance
        if (!isApi || !_enableBodyLogging)
        {
            await next(context);
            stopwatch.Stop();
            LogMinimal(context, stopwatch.Elapsed.TotalMilliseconds);
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
            stopwatch.Stop();

            // 3. Read Response Body
            responseBodyWrapper.Position = 0;
            var responseBodyText = await ReadStreamAsync(responseBodyWrapper);
            
            // 4. Restore original stream
            responseBodyWrapper.Position = 0;
            await responseBodyWrapper.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;

            // 5. Unified Log
            LogFull(context, stopwatch.Elapsed.TotalMilliseconds, requestBodyText, responseBodyText);
        }
    }

    private void LogMinimal(HttpContext context, double elapsedMs)
    {
        var statusCode = context.Response.StatusCode;
        var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Anonymous";
        var correlationId = context.GetCorrelationId();
        
        var level = statusCode >= 500 ? LogLevel.Error : (elapsedMs > 500 ? LogLevel.Warning : LogLevel.Information);

        logger.Log(level, 
            "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs:F3}ms | User: {UserId} | CID: {CorrelationId}",
            context.Request.Method, context.Request.Path, statusCode, elapsedMs, userId, correlationId);
    }

    private void LogFull(HttpContext context, double elapsedMs, string reqBody, string resBody)
    {
        var statusCode = context.Response.StatusCode;
        var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Anonymous";
        var correlationId = context.GetCorrelationId();
        
        var level = statusCode >= 500 ? LogLevel.Error : (elapsedMs > 500 ? LogLevel.Warning : LogLevel.Information);

        // We pass the parsed objects directly to Serilog. 
        // By using Dictionary<string, object>, Serilog destructures correctly for Seq.
        logger.Log(level, 
            "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs:F3}ms | User: {UserId} | CID: {CorrelationId} | Req: {@RequestBody} | Res: {@ResponseBody}",
            context.Request.Method, 
            context.Request.Path, 
            statusCode, 
            elapsedMs, 
            userId, 
            correlationId,
            TryParseJson(reqBody) ?? "N/A",
            TryParseJson(resBody) ?? "N/A");
    }

    private static object? TryParseJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        try
        {
            using var doc = JsonDocument.Parse(text);
            return ToObject(doc.RootElement);
        }
        catch
        {
            return text;
        }
    }

    private static object? ToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ToObject(p.Value)),
            JsonValueKind.Array => element.EnumerateArray().Select(ToObject).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out long l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
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
