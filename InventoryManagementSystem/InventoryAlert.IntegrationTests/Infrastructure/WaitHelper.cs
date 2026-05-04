namespace InventoryAlert.IntegrationTests.Infrastructure;

public static class WaitHelper
{
    public static async Task WaitForConditionAsync(Func<Task<bool>> condition, int timeoutSeconds = 30, int pollIntervalMs = 2000)
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < TimeSpan.FromSeconds(timeoutSeconds))
        {
            if (await condition()) return;
            await Task.Delay(pollIntervalMs);
        }
        
        throw new TimeoutException($"Condition was not met within {timeoutSeconds} seconds.");
    }
}
