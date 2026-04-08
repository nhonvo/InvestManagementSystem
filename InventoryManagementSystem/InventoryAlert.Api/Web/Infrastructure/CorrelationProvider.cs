using InventoryAlert.Api.Application.Interfaces;

namespace InventoryAlert.Api.Web.Infrastructure;

public class CorrelationProvider(IHttpContextAccessor httpContextAccessor) : ICorrelationProvider
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public string GetCorrelationId()
    {
        var context = httpContextAccessor.HttpContext;
        if (context == null) return Guid.NewGuid().ToString();

        if (context.Items.TryGetValue(CorrelationIdHeader, out var cid) && cid != null)
        {
            return cid.ToString()!;
        }

        return Guid.NewGuid().ToString();
    }
}
