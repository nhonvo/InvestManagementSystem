using InventoryAlert.Worker.Application.Models;

namespace InventoryAlert.Worker.Infrastructure.Jobs;

public class ProfileSyncJob(ILogger<ProfileSyncJob> logger)
{
    public async Task<JobResult> ExecuteAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(10, ct); // Simulating work
            logger.LogInformation("ProfileSyncJob completed");
            return new JobResult(JobStatus.Success);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute ProfileSyncJob");
            return new JobResult(JobStatus.Failed, Error: ex);
        }
    }
}
