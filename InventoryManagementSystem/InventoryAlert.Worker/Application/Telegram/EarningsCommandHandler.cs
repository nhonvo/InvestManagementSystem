using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryAlert.Worker.Application.Telegram;

public class EarningsCommandHandler(ILogger<EarningsCommandHandler> logger)
{
    public Task HandleAsync(string args, CancellationToken ct = default)
    {
        logger.LogInformation("Earnings command called with args: {Args}", args);
        return Task.CompletedTask;
    }
}
