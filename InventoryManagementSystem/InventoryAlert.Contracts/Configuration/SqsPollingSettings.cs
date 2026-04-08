namespace InventoryAlert.Contracts.Configuration;

public class SqsPollingSettings
{
    public bool UseNativeWorker { get; set; } = false;
    public int QueueCapacity { get; set; } = 10;
}
