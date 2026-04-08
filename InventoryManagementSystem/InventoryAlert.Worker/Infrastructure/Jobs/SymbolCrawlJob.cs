using InventoryAlert.Worker.Application.Models;
using Microsoft.Extensions.Logging;

namespace InventoryAlert.Worker.Infrastructure.Jobs;

public class SymbolCrawlJob(ILogger<SymbolCrawlJob> logger)
{
    public async Task<JobResult> ExecuteAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(10, ct); // Simulating work
            logger.LogInformation("SymbolCrawlJob completed");
            return new JobResult(JobStatus.Success);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute SymbolCrawlJob");
            return new JobResult(JobStatus.Failed, Error: ex);
        }
    }
}
