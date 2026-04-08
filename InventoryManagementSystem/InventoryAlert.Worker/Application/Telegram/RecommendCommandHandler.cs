namespace InventoryAlert.Worker.Application.Telegram;

public class RecommendCommandHandler(ILogger<RecommendCommandHandler> logger)
{
    public Task HandleAsync(string args, CancellationToken ct = default)
    {
        logger.LogInformation("Recommend command called with args: {Args}", args);
        return Task.CompletedTask;
    }
}
