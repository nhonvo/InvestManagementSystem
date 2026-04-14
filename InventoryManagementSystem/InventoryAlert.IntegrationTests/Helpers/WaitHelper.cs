namespace InventoryAlert.IntegrationTests.Helpers;

public static class WaitHelper
{
    public static async Task<T> WaitForConditionAsync<T>(Func<Task<T>> action, TimeSpan timeout, TimeSpan pollingInterval)
    {
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < timeout)
        {
            var result = await action();
            if (result != null)
                return result;

            await Task.Delay(pollingInterval);
        }

        throw new TimeoutException("The condition was not met within the specified timeout.");
    }
}
