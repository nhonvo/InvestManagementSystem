using InventoryAlert.Contracts.Configuration;

namespace InventoryAlert.Worker.Configuration;

public class WorkerSettings
{
    public SharedDatabaseSettings Database { get; set; } = new();
    public RedisSetting Redis { get; set; } = new();
    public SharedAwsSettings Aws { get; set; } = new();
    public SharedFinnhubSettings Finnhub { get; set; } = new();
    public SqsPollingSettings SqsPolling { get; set; } = new();
}

public class RedisSetting
{
    public string Connection { get; set; } = string.Empty;
}

public class SqsPollingSettings
{
    /// <summary>
    /// True (Default): Use the continuous ProcessQueueJob (Native Worker).
    /// False: Use the Hangfire-scheduled PollSqsJob (Recurring).
    /// </summary>
    public bool UseNativeWorker { get; set; } = true;
    public int QueueCapacity { get; internal set; } = 10;
}
