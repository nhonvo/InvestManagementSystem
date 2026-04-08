using InventoryAlert.Worker.Application.Models;

namespace InventoryAlert.Worker.Infrastructure.Jobs;

public class EarningsJob(ILogger<EarningsJob> logger)
{
    public async Task<JobResult> ExecuteAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(10, ct); // Simulating work
            logger.LogInformation("EarningsJob completed");
            return new JobResult(JobStatus.Success);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute EarningsJob");
            return new JobResult(JobStatus.Failed, Error: ex);
        }
    }
}
