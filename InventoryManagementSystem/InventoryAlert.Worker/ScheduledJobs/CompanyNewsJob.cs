using InventoryAlert.Domain.Entities.Dynamodb;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Worker.Models;

namespace InventoryAlert.Worker.ScheduledJobs;

public class CompanyNewsJob(
    IUnitOfWork unitOfWork,
    IFinnhubClient finnhub,
    ICompanyNewsDynamoRepository newsRepo,
    ILogger<CompanyNewsJob> logger)
{
    public async Task<JobResult> ExecuteAsync(CancellationToken ct)
    {
        try
        {
            var listings = await unitOfWork.StockListings.GetAllAsync(ct);
            var to = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var from = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
            int totalSaved = 0;

            foreach (var listing in listings)
            {
                var articles = await finnhub.GetCompanyNewsAsync(listing.TickerSymbol, from, to, ct);
                if (articles.Count == 0) continue;

                var entries = articles
                    .DistinctBy(a => a.Id)
                    .Select(a => new CompanyNewsDynamoEntry
                {
                    PK = $"SYMBOL#{listing.TickerSymbol.ToUpperInvariant()}",
                    SK = $"TS#{a.Datetime}#ID#{a.Id}",
                    Symbol = listing.TickerSymbol,
                    Timestamp = a.Datetime,
                    Headline = a.Headline ?? "No Headline",
                    Summary = a.Summary ?? string.Empty,
                    Source = a.Source ?? "Unknown",
                    Url = a.Url ?? string.Empty,
                    ImageUrl = a.Image ?? string.Empty,
                    NewsId = a.Id,
                    SyncedAt = DateTime.UtcNow.ToString("O")
                }).ToList();

                await newsRepo.BatchSaveAsync(entries, ct);
                totalSaved += entries.Count;
            }

            return new JobResult(JobStatus.Success, $"Persisted {totalSaved} company news articles.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CompanyNewsJob] Pipeline failure.");
            return new JobResult(JobStatus.Failed, "Error in CompanyNewsJob.", Error: ex);
        }
    }
}
