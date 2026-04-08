using InventoryAlert.Contracts.Events.Payloads;
using InventoryAlert.Worker.Application.Interfaces.Handlers;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryAlert.Worker.Application.IntegrationHandlers;

public class EarningsHandler(ILogger<EarningsHandler> logger) : IEventHandler<EarningsPayload>
{
    public Task HandleAsync(EarningsPayload payload, CancellationToken ct = default)
    {
        logger.LogInformation("Handled Earnings for {Symbol}", payload.Symbol);
        return Task.CompletedTask;
    }
}
