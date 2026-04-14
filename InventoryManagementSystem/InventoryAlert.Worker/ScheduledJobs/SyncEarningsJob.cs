using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Worker.Models;

namespace InventoryAlert.Worker.ScheduledJobs;

public class SyncEarningsJob(
    IUnitOfWork unitOfWork,
    IFinnhubClient finnhub,
    ILogger<SyncEarningsJob> logger)
{
    public async Task<JobResult> ExecuteAsync(CancellationToken ct)
    {
        try
        {
            var listings = await unitOfWork.StockListings.GetAllAsync(ct);
            foreach (var listing in listings)
            {
                var surprise = await finnhub.GetEarningsAsync(listing.TickerSymbol, ct);
                if (surprise == null || surprise.Count == 0) continue;

                var data = surprise.Select(s => new EarningsSurprise
                {
                    TickerSymbol = listing.TickerSymbol,
                    Period = DateOnly.TryParse(s.Period, out var p) ? p : DateOnly.MinValue,
                    ActualEps = s.Actual,
                    EstimateEps = s.Estimate,
                    SurprisePercent = s.SurprisePercent,
                    ReportDate = DateOnly.TryParse(s.ReportDate, out var rd) ? rd : null
                });

                await unitOfWork.Earnings.UpsertRangeAsync(data, ct);
            }

            await unitOfWork.SaveChangesAsync(ct);
            return new JobResult(JobStatus.Success, "Earnings surprise sync completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[SyncEarningsJob] Execution failure.");
            return new JobResult(JobStatus.Failed, "Failed to sync earnings.", Error: ex);
        }
    }
}
