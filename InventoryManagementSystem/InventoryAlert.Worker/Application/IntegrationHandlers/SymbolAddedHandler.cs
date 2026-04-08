using InventoryAlert.Contracts.Events.Payloads;
using InventoryAlert.Worker.Application.Interfaces.Handlers;

namespace InventoryAlert.Worker.Application.IntegrationHandlers;

public class SymbolAddedHandler(ILogger<SymbolAddedHandler> logger) : IEventHandler<SymbolAddedPayload>
{
    public Task HandleAsync(SymbolAddedPayload payload, CancellationToken ct = default)
    {
        logger.LogInformation("Handled SymbolAdded for {Symbol}", payload.Symbol);
        return Task.CompletedTask;
    }
}
