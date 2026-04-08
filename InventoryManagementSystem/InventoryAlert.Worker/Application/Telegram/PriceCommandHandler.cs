using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryAlert.Worker.Application.Telegram;

public class PriceCommandHandler(ILogger<PriceCommandHandler> logger)
{
    public Task HandleAsync(string args, CancellationToken ct = default)
    {
        logger.LogInformation("Price command called with args: {Args}", args);
        return Task.CompletedTask;
    }
}
