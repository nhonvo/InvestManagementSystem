using InventoryAlert.Domain.Interfaces;
using Serilog.Core;
using Serilog.Events;

namespace InventoryAlert.Infrastructure.Utilities;

/// <summary>
/// Serilog enricher that uses ICorrelationProvider to attach the current CorrelationId to every log entry.
/// This ensures logs in both API (via Middleware) and Worker (via AsyncLocal) are traceable in Seq and Console.
/// </summary>
public class CorrelationIdEnricher(ICorrelationProvider correlationProvider) : ILogEventEnricher
{
    private readonly ICorrelationProvider _correlationProvider = correlationProvider;

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // 1. If property already exists and has a real value, don't overwrite it.
        // This allows ILogger.BeginScope or LogContext.PushProperty to take precedence.
        if (logEvent.Properties.TryGetValue("CorrelationId", out var existing))
        {
            var value = existing.ToString().Trim('\"');
            if (!string.IsNullOrEmpty(value) && value != "N/A")
            {
                return;
            }
        }

        var correlationId = _correlationProvider.GetCorrelationId();
        var property = propertyFactory.CreateProperty("CorrelationId", correlationId);
        logEvent.AddOrUpdateProperty(property);
    }
}
