namespace InventoryAlert.Domain.Configuration;

public class AppSettings
{
    public SharedAwsSettings Aws { get; set; } = new();
    public SharedDatabaseSettings Database { get; set; } = new();
    public SharedFinnhubSettings Finnhub { get; set; } = new();
    public RedisSettings Redis { get; set; } = new();
    public SharedSeqSettings Seq { get; set; } = new();
}
