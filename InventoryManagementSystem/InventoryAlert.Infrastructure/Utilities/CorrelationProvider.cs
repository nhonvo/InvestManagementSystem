using InventoryAlert.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace InventoryAlert.Infrastructure.Utilities;

public class CorrelationProvider(IHttpContextAccessor httpContextAccessor) : ICorrelationProvider
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private static readonly AsyncLocal<string?> _currentCorrelationId = new();

    public string GetCorrelationId()
    {
        // 1. Prefer explicitly set Correlation ID (for workers/async flows)
        var cid = _currentCorrelationId.Value;
        if (!string.IsNullOrEmpty(cid))
        {
            return cid;
        }

        // 2. Fallback to HttpContext
        var context = httpContextAccessor.HttpContext;
        if (context != null && context.Items.TryGetValue(CorrelationIdHeader, out var httpCid) && httpCid != null)
        {
            return httpCid.ToString()!;
        }

        // 3. Last resort: Generate new one
        return "N/A";
    }

    public void SetCorrelationId(string correlationId)
    {
        _currentCorrelationId.Value = correlationId;
    }
}
