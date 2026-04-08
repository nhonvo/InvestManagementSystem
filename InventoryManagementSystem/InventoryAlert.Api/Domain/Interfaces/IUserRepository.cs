using InventoryAlert.Contracts.Entities;

namespace InventoryAlert.Api.Domain.Interfaces;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<bool> ExistsAsync(string username, CancellationToken ct = default);
}
