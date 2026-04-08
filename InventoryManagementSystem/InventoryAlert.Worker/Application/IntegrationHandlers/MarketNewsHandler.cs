using InventoryAlert.Contracts.Events.Payloads;
using InventoryAlert.Worker.Application.Interfaces.Handlers;

namespace InventoryAlert.Worker.Application.IntegrationHandlers;

public class MarketNewsHandler(ILogger<MarketNewsHandler> logger) : IEventHandler<MarketNewsPayload>
{
    public Task HandleAsync(MarketNewsPayload payload, CancellationToken ct = default)
    {
        logger.LogInformation("Handled MarketNews for {Headline}", payload.Headline);
        return Task.CompletedTask;
    }
}
