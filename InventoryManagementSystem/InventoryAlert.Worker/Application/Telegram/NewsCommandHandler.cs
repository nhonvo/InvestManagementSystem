namespace InventoryAlert.Worker.Application.Telegram;

public class NewsCommandHandler(ILogger<NewsCommandHandler> logger)
{
    public Task HandleAsync(string args, CancellationToken ct = default)
    {
        logger.LogInformation("News command called with args: {Args}", args);
        return Task.CompletedTask;
    }
}
