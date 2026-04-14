using Hangfire.Dashboard;

namespace InventoryAlert.Worker;

/// <summary>
/// Permissive authorization filter for Docker/Development environments.
/// In production, this should be replaced with a real Identity-based check.
/// </summary>
public class DevDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // Allow all access in development or docker for easier debugging.
        return true;
    }
}
