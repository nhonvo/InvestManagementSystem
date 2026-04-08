namespace InventoryAlert.Contracts.Configuration;

public class SharedDatabaseSettings
{
    public string DefaultConnection { get; set; } = string.Empty;
}

public class RedisSettings
{
    public string ConnectionString { get; set; } = "localhost:6379";
}

public class SharedFinnhubSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public int SyncIntervalMinutes { get; set; } = 30;
}
