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

            foreach (var listing in listings)
            {
                var quote = await _finnhub.GetQuoteAsync(listing.TickerSymbol, ct);
                if (quote?.CurrentPrice is null or 0) continue;

                // 1. Record History
                await _unitOfWork.PriceHistories.AddAsync(new PriceHistory
                {
                    TickerSymbol = listing.TickerSymbol,
                    RecordedAt = DateTime.UtcNow,
                    Open = quote.OpenPrice ?? 0,
                    High = quote.HighPrice ?? 0,
                    Low = quote.LowPrice ?? 0,
                    Price = quote.CurrentPrice.Value
                }, ct);

                // 2. Fetch Active Rules
                var rules = await _unitOfWork.AlertRules.GetBySymbolAsync(listing.TickerSymbol, ct);

                foreach (var rule in rules)
                {
                    bool breached = false;
                    string message = "";

                    if (rule.Condition == AlertCondition.PriceAbove && quote.CurrentPrice > rule.TargetValue)
                    {
                        breached = true;
                        message = $"{listing.TickerSymbol} price {quote.CurrentPrice:C} is above your target {rule.TargetValue:C}";
                    }
                    else if (rule.Condition == AlertCondition.PriceBelow && quote.CurrentPrice < rule.TargetValue)
                    {
                        breached = true;
                        message = $"{listing.TickerSymbol} price {quote.CurrentPrice:C} is below your target {rule.TargetValue:C}";
                    }
                    else if (rule.Condition == AlertCondition.PercentDropFromCost)
                    {
                        var trades = await _unitOfWork.Trades.GetByUserAndSymbolAsync(rule.UserId, listing.TickerSymbol, ct);
                        var totalCost = trades.Where(t => t.Type == TradeType.Buy).Sum(t => t.Quantity * t.UnitPrice);
                        var totalQty = trades.Where(t => t.Type == TradeType.Buy).Sum(t => t.Quantity);
                        var avgCost = totalQty > 0 ? totalCost / totalQty : 0;

                        if (avgCost > 0)
                        {
                            var drop = (avgCost - quote.CurrentPrice.Value) / avgCost * 100;
                            if (drop >= rule.TargetValue)
                            {
                                breached = true;
                                message = $"{listing.TickerSymbol} has dropped {drop:F2}% from your cost basis of {avgCost:C}";
                            }
                        }
                    }

                    if (breached)
                    {
                        var notification = new Notification
                        {
                            UserId = rule.UserId,
                            Message = message,
                            TickerSymbol = listing.TickerSymbol,
                            AlertRuleId = rule.Id,
                            IsRead = false
                        };

                        await _unitOfWork.Notifications.AddAsync(notification, ct);
                        await _notifier.NotifyAsync(notification, ct);

                        if (rule.TriggerOnce) rule.IsActive = false;
                        rule.LastTriggeredAt = DateTime.UtcNow;
                    }
                }
                success++;
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return new JobResult(JobStatus.Success, $"Synced {success} symbol prices.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SyncPrices] Execution failure.");
            return new JobResult(JobStatus.Failed, "Critical failure in price sync engine.", Error: ex);
        }
    }
}
