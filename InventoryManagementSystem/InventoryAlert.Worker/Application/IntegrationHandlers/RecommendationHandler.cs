using InventoryAlert.Contracts.Events.Payloads;
using InventoryAlert.Worker.Application.Interfaces.Handlers;

namespace InventoryAlert.Worker.Application.IntegrationHandlers;

public class RecommendationHandler(ILogger<RecommendationHandler> logger) : IEventHandler<RecommendationUpdatedPayload>
{
    public Task HandleAsync(RecommendationUpdatedPayload payload, CancellationToken ct = default)
    {
        logger.LogInformation("Handled Recommendation for {Symbol}", payload.Symbol);
        return Task.CompletedTask;
    }
}
