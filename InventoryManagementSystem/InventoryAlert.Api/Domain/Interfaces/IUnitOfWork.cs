namespace InventoryAlert.Api.Domain.Interfaces
{
    public interface IUnitOfWork
    {
        Task SaveChangesAsync(CancellationToken cancellationToken);
        Task ExecuteTransactionAsync(Action action, CancellationToken cancellationToken);
        Task ExecuteTransactionAsync(Func<Task> action, CancellationToken cancellationToken);
    }
}
