using InventoryAlert.Contracts.Entities;
using InventoryAlert.Contracts.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Contracts.Persistence.Repositories;

public class UserRepository(InventoryDbContext context)
    : GenericRepository<User>(context), IUserRepository
{
    private readonly InventoryDbContext _context = context;

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username, ct);
    }

    public async Task<bool> ExistsAsync(string username, CancellationToken ct = default)
    {
        return await _context.Users
            .AnyAsync(u => u.Username == username, ct);
    }
}
