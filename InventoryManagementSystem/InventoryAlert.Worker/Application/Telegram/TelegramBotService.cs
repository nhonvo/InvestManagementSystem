using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

// Assuming Telegram.Bot exists
namespace Telegram.Bot.Types 
{
    public class Update { }
}

namespace InventoryAlert.Worker.Application.Telegram
{
    public class TelegramBotService(ILogger<TelegramBotService> logger)
    {
        public Task DispatchAsync(global::Telegram.Bot.Types.Update update, CancellationToken ct = default)
        {
            logger.LogInformation("Dispatching telegram update");
            return Task.CompletedTask;
        }
    }
}
