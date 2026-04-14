using InventoryAlert.Domain.Configuration;

namespace InventoryAlert.Worker.Configuration;

public class WorkerSettings : AppSettings
{
    public bool IsPollMessage { get; set; } = true;

    public JobSchedules Schedules { get; set; } = new();
}

public class JobSchedules
{
    public string SyncPrices { get; set; } = "*/5 * * * *"; // Every 5 min
    public string SyncMetrics { get; set; } = "0 8 * * *"; // Daily 8 AM
    public string SyncEarnings { get; set; } = "0 9 * * *"; // Daily 9 AM
    public string SyncRecommendations { get; set; } = "0 10 * * *"; // Daily 10 AM
    public string SyncInsiders { get; set; } = "0 11 * * *"; // Daily 11 AM
    public string NewsCheck { get; set; } = "*/15 * * * *"; // Every 15 min
    public string MarketNews { get; set; } = "*/30 * * * *"; // Every 30 min
    public string CleanupPrices { get; set; } = "0 2 * * *"; // Daily 2 AM
}
