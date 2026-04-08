using InventoryAlert.Worker.Application.Models;
using Microsoft.Extensions.Logging;

namespace InventoryAlert.Worker.Infrastructure.Jobs;

public class MarketStatusJob(ILogger<MarketStatusJob> logger)
{
    public async Task<JobResult> ExecuteAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(10, ct); // Simulating work
            logger.LogInformation("MarketStatusJob completed");
            return new JobResult(JobStatus.Success);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute MarketStatusJob");
            return new JobResult(JobStatus.Failed, Error: ex);
        }
    }
}
