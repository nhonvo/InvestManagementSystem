namespace InventoryAlert.Worker.Interfaces;

public interface IProcessQueueJob
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}
