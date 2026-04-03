namespace InventoryAlert.Api.Infrastructure.Persistence.Interfaces
{
    public interface IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken);
        public Task ExecuteTransactionAsync(Action action, CancellationToken cancellationToken);
        public Task ExecuteTransactionAsync(Func<Task> action, CancellationToken token);
    }
}
