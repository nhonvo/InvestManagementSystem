using InventoryAlert.Domain.Entities.Postgres;

namespace InventoryAlert.Domain.Interfaces;

public interface IAlertRuleEvaluator
{
    /// <summary>
    /// Evaluates an alert rule against a current price.
    /// Returns a tuple with (bool isBreached, string message).
    /// </summary>
    Task<(bool IsBreached, string Message)> EvaluateAsync(AlertRule rule, decimal currentPrice, CancellationToken ct);
}
