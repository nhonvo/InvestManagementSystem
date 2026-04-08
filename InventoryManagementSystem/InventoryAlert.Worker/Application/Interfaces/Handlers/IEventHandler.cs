using System.Threading;
using System.Threading.Tasks;

namespace InventoryAlert.Worker.Application.Interfaces.Handlers;

public interface IEventHandler<in TPayload>
{
    Task HandleAsync(TPayload payload, CancellationToken ct = default);
}
