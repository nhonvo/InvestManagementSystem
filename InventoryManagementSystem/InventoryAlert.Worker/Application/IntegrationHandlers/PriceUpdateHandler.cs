using InventoryAlert.Contracts.Events.Payloads;
using InventoryAlert.Worker.Application.Interfaces.Handlers;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryAlert.Worker.Application.IntegrationHandlers;

public class PriceUpdateHandler(ILogger<PriceUpdateHandler> logger) : IEventHandler<PriceUpdatePayload>
{
    public Task HandleAsync(PriceUpdatePayload payload, CancellationToken ct = default)
    {
        logger.LogInformation("Handled PriceUpdate for {Symbol}", payload.TickerSymbol);
        return Task.CompletedTask;
    }
}
