using InventoryAlert.Worker.Application.Models;

namespace InventoryAlert.Worker.Infrastructure.Jobs;

public class MarketNewsJob(ILogger<MarketNewsJob> logger)
{
    public async Task<JobResult> ExecuteAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(10, ct); // Simulating work
            logger.LogInformation("MarketNewsJob completed");
            return new JobResult(JobStatus.Success);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute MarketNewsJob");
            return new JobResult(JobStatus.Failed, Error: ex);
        }
    }
}
