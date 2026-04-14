using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Worker.Models;

namespace InventoryAlert.Worker.ScheduledJobs;

public class SyncRecommendationsJob(
    IUnitOfWork unitOfWork,
    IFinnhubClient finnhub,
    ILogger<SyncRecommendationsJob> logger)
{
    public async Task<JobResult> ExecuteAsync(CancellationToken ct)
    {
        try
        {
            var listings = await unitOfWork.StockListings.GetAllAsync(ct);
            foreach (var listing in listings)
            {
                var trends = await finnhub.GetRecommendationsAsync(listing.TickerSymbol, ct);
                if (trends == null || trends.Count == 0) continue;

                var data = trends.Select(t => new RecommendationTrend
                {
                    TickerSymbol = listing.TickerSymbol,
                    Period = t.Period ?? "N/A",
                    StrongBuy = t.StrongBuy,
                    Buy = t.Buy,
                    Hold = t.Hold,
                    Sell = t.Sell,
                    StrongSell = t.StrongSell
                });

                await unitOfWork.Recommendations.UpsertRangeAsync(data, ct);
            }

            await unitOfWork.SaveChangesAsync(ct);
            return new JobResult(JobStatus.Success, "Analyst recommendations sync completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[SyncRecommendationsJob] Execution failure.");
            return new JobResult(JobStatus.Failed, "Failed to sync recommendations.", Error: ex);
        }
    }
}
