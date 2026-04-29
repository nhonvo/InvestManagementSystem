using InventoryAlert.Domain.Common.Constants;
using InventoryAlert.Domain.Constants;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace InventoryAlert.Infrastructure.Utilities;

public class AlertRuleEvaluator(
    IUnitOfWork unitOfWork,
    IRedisHelper redis,
    ILogger<AlertRuleEvaluator> logger) : IAlertRuleEvaluator
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IRedisHelper _redis = redis;
    private readonly ILogger<AlertRuleEvaluator> _logger = logger;

    public async Task<(bool IsBreached, string Message)> EvaluateAsync(AlertRule rule, decimal currentPrice, CancellationToken ct)
    {
        // 1. Check Cooldown/Deduplication
        var cooldownKey = CacheKeys.AlertCooldown(rule.UserId, rule.Id);
        if (await _redis.KeyExistsAsync(cooldownKey, ct))
        {
            return (false, string.Empty);
        }

        bool isBreached = false;
        string message = string.Empty;

        switch (rule.Condition)
        {
            case AlertCondition.PriceAbove:
                if (currentPrice > rule.TargetValue)
                {
                    isBreached = true;
                    message = $"{rule.TickerSymbol} price {currentPrice:C} is above your target {rule.TargetValue:C}";
                }
                break;

            case AlertCondition.PriceBelow:
                if (currentPrice < rule.TargetValue)
                {
                    isBreached = true;
                    message = $"{rule.TickerSymbol} price {currentPrice:C} is below your target {rule.TargetValue:C}";
                }
                break;

            case AlertCondition.PriceTargetReached:
                if (Math.Abs(currentPrice - rule.TargetValue) < 0.01m)
                {
                    isBreached = true;
                    message = $"{rule.TickerSymbol} price reached your target {rule.TargetValue:C}";
                }
                break;

            case AlertCondition.PercentDropFromCost:
                var avgCost = await GetAverageCostAsync(rule.UserId, rule.TickerSymbol, ct);
                if (avgCost > 0)
                {
                    var drop = (avgCost - currentPrice) / avgCost * 100;
                    if (drop >= rule.TargetValue)
                    {
                        isBreached = true;
                        message = $"{rule.TickerSymbol} has dropped {drop:F2}% from your cost basis of {avgCost:C}";
                    }
                }
                break;
        }

        if (isBreached)
        {
            // Set cooldown to prevent alert storm (e.g., 24 hours)
            await _redis.TryAcquireBestEffortLockAsync(cooldownKey, "1", TimeSpan.FromHours(24), ct);
            _logger.LogInformation("[Evaluator] Rule {RuleId} breached for user {UserId}", rule.Id, rule.UserId);
        }

        return (isBreached, message);
    }

    private async Task<decimal> GetAverageCostAsync(Guid userId, string symbol, CancellationToken ct)
    {
        try
        {
            var trades = await _unitOfWork.Trades.GetByUserAndSymbolAsync(userId, symbol, ct);
            var buys = trades.Where(t => t.Type == TradeType.Buy).ToList();
            var totalCost = buys.Sum(t => t.Quantity * t.UnitPrice);
            var totalQty = buys.Sum(t => t.Quantity);
            return totalQty > 0 ? totalCost / totalQty : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate average cost for User {UserId} Symbol {Symbol}", userId, symbol);
            return 0;
        }
    }
}
