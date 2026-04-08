using InventoryAlert.Contracts.Configuration;

namespace InventoryAlert.Worker.Configuration;

public class WorkerSettings
{
    public SharedDatabaseSettings Database { get; set; } = new();
    public RedisSettings Redis { get; set; } = new();
    public SharedAwsSettings Aws { get; set; } = new();
    public SharedFinnhubSettings Finnhub { get; set; } = new();
    public SqsPollingSettings SqsPolling { get; set; } = new();
    public SharedSeqSettings Seq { get; set; } = new();
}
