namespace InventoryAlert.Domain.Configuration;

public class SharedFinnhubSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public int SyncIntervalMinutes { get; set; } = 30;
}

