using InventoryAlert.Contracts.Events.Payloads;
using InventoryAlert.Worker.Application.Interfaces.Handlers;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryAlert.Worker.Application.IntegrationHandlers;

public class AlertRuleHandler(ILogger<AlertRuleHandler> logger) : IEventHandler<AlertRulePayload>
{
    public Task HandleAsync(AlertRulePayload payload, CancellationToken ct = default)
    {
        logger.LogInformation("Handled AlertRule for {Symbol}", payload.Symbol);
        return Task.CompletedTask;
    }
}
