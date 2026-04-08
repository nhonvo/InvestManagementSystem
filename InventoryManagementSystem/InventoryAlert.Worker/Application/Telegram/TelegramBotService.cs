// Assuming Telegram.Bot exists
namespace InventoryAlert.Worker.Application.Telegram
{
    public class Update { }
}

namespace InventoryAlert.Worker.Application.Telegram
{
    public class TelegramBotService(ILogger<TelegramBotService> logger)
    {
        public Task DispatchAsync(global::InventoryAlert.Worker.Application.Telegram.Update update, CancellationToken ct = default)
        {
            logger.LogInformation("Dispatching telegram update");
            return Task.CompletedTask;
        }
    }
}
