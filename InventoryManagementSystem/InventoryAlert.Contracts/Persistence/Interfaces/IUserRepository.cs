using InventoryAlert.Contracts.Entities;

namespace InventoryAlert.Contracts.Persistence.Interfaces;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<bool> ExistsAsync(string username, CancellationToken ct = default);
}
