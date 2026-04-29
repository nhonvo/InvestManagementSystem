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
        if (!string.IsNullOrEmpty(_currentCorrelationId.Value))
        {
            return _currentCorrelationId.Value;
        }

        // 2. Fallback to HttpContext
        var context = httpContextAccessor.HttpContext;
        if (context != null && context.Items.TryGetValue(CorrelationIdHeader, out var cid) && cid != null)
        {
            return cid.ToString()!;
        }

        // 3. Last resort: Generate new one
        return Guid.NewGuid().ToString();
    }

    public void SetCorrelationId(string correlationId)
    {
        _currentCorrelationId.Value = correlationId;
    }
}
