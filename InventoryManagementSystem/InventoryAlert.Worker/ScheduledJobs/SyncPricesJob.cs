using System.Collections.Concurrent;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Worker.Models;

namespace InventoryAlert.Worker.ScheduledJobs;

public class SyncPricesJob(
    IUnitOfWork unitOfWork,
    IFinnhubClient finnhub,
    IAlertNotifier notifier,
    ILogger<SyncPricesJob> logger)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IFinnhubClient _finnhub = finnhub;
    private readonly IAlertNotifier _notifier = notifier;
    private readonly ILogger<SyncPricesJob> _logger = logger;

    public async Task<JobResult> ExecuteAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("[SyncPrices] Starting price synchronization...");

            var listings = await _unitOfWork.StockListings.GetAllAsync(ct);
            int success = 0;

            var fetchedQuotes = new ConcurrentDictionary<string, decimal>();
            var newPriceHistories = new ConcurrentBag<PriceHistory>();
            var pendingNotifications = new List<Notification>();

            // PART 1: Sync Price (Enhanced with Parallelism)
            // Using a concurrency limit to prevent hitting API rate limits too hard
            var options = new ParallelOptions { MaxDegreeOfParallelism = 5, CancellationToken = ct };
            await Parallel.ForEachAsync(listings, options, async (listing, token) =>
            {
                try
                {
                    var quote = await _finnhub.GetQuoteAsync(listing.TickerSymbol, token);
                    if (quote?.CurrentPrice is null or 0) return;

                    newPriceHistories.Add(new PriceHistory
                    {
                        TickerSymbol = listing.TickerSymbol,
                        RecordedAt = DateTime.UtcNow,
                        Open = quote.OpenPrice ?? 0,
                        High = quote.HighPrice ?? 0,
                        Low = quote.LowPrice ?? 0,
                        Price = quote.CurrentPrice.Value
                    });

                    fetchedQuotes[listing.TickerSymbol] = quote.CurrentPrice.Value;
                    Interlocked.Increment(ref success);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("[SyncPrices] Failed to fetch quote for {Symbol}: {Msg}", listing.TickerSymbol, ex.Message);
                }
            });

            if (!newPriceHistories.IsEmpty)
            {
                await _unitOfWork.PriceHistories.AddRangeAsync(newPriceHistories, ct);
            }

            // PART 2: Check Alerts (Enhanced with Batch Fetching and Memoization)
            var symbolsToProcess = fetchedQuotes.Keys.ToList();
            var allActiveRules = await _unitOfWork.AlertRules.GetBySymbolsAsync(symbolsToProcess, ct);
            var rulesBySymbol = allActiveRules.GroupBy(r => r.TickerSymbol).ToDictionary(g => g.Key, g => g.ToList());
            
            // Local cache to avoid re-calculating cost basis for multiple alerts on the same User+Symbol
            var tradeBasisCache = new Dictionary<(Guid UserId, string Symbol), decimal>();

            foreach (var kvp in fetchedQuotes)
            {
                var symbol = kvp.Key;
                var currentPrice = kvp.Value;

                if (!rulesBySymbol.TryGetValue(symbol, out var rules)) continue;

                foreach (var rule in rules)
                {
                    bool breached = false;
                    string message = "";

                    if (rule.Condition == AlertCondition.PriceAbove && currentPrice > rule.TargetValue)
                    {
                        breached = true;
                        message = $"{symbol} price {currentPrice:C} is above your target {rule.TargetValue:C}";
                    }
                    else if (rule.Condition == AlertCondition.PriceBelow && currentPrice < rule.TargetValue)
                    {
                        breached = true;
                        message = $"{symbol} price {currentPrice:C} is below your target {rule.TargetValue:C}";
                    }
                    else if (rule.Condition == AlertCondition.PercentDropFromCost)
                    {
                        if (!tradeBasisCache.TryGetValue((rule.UserId, symbol), out var avgCost))
                        {
                            var trades = await _unitOfWork.Trades.GetByUserAndSymbolAsync(rule.UserId, symbol, ct);
                            var buys = trades.Where(t => t.Type == TradeType.Buy).ToList();
                            var totalCost = buys.Sum(t => t.Quantity * t.UnitPrice);
                            var totalQty = buys.Sum(t => t.Quantity);
                            avgCost = totalQty > 0 ? totalCost / totalQty : 0;
                            tradeBasisCache[(rule.UserId, symbol)] = avgCost;
                        }

                        if (avgCost > 0)
                        {
                            var drop = (avgCost - currentPrice) / avgCost * 100;
                            if (drop >= rule.TargetValue)
                            {
                                breached = true;
                                message = $"{symbol} has dropped {drop:F2}% from your cost basis of {avgCost:C}";
                            }
                        }
                    }

                    if (breached)
                    {
                        var notification = new Notification
                        {
                            UserId = rule.UserId,
                            Message = message,
                            TickerSymbol = symbol,
                            AlertRuleId = rule.Id,
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        };

                        pendingNotifications.Add(notification);

                        if (rule.TriggerOnce) rule.IsActive = false;
                        rule.LastTriggeredAt = DateTime.UtcNow;
                    }
                }
            }

            // PART 3: Notify
            if (pendingNotifications.Count > 0)
            {
                await _unitOfWork.Notifications.AddRangeAsync(pendingNotifications, ct);
                
                foreach (var notification in pendingNotifications)
                {
                    // Fire-and-forget or sequential depending on notifier capacity
                    await _notifier.NotifyAsync(notification, ct);
                }
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return new JobResult(JobStatus.Success, $"Synced {success} symbol prices and processed alerts.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SyncPrices] Execution failure.");
            return new JobResult(JobStatus.Failed, "Critical failure in price sync engine.", Error: ex);
        }
    }
}
