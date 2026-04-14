using Amazon.SQS.Model;

namespace InventoryAlert.Worker.Interfaces;

public interface IDefaultHandler<in TPayload>
{
    Task HandleAsync(TPayload payload, CancellationToken ct = default);
}

public interface IRawDefaultHandler : IDefaultHandler<Message>
{
}
