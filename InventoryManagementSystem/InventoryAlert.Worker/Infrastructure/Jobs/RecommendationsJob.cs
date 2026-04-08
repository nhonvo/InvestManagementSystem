using InventoryAlert.Worker.Application.Models;

namespace InventoryAlert.Worker.Infrastructure.Jobs;

public class RecommendationsJob(ILogger<RecommendationsJob> logger)
{
    public async Task<JobResult> ExecuteAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(10, ct); // Simulating work
            logger.LogInformation("RecommendationsJob completed");
            return new JobResult(JobStatus.Success);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute RecommendationsJob");
            return new JobResult(JobStatus.Failed, Error: ex);
        }
    }
}
