namespace InventoryAlert.Domain.Interfaces;

public interface IRedisHelper
{
    Task<bool> TryAcquireLockAsync(string key, string value, TimeSpan expiry, CancellationToken ct = default);
    Task SetExpiryAsync(string key, TimeSpan expiry, CancellationToken ct = default);
    Task<bool> KeyExistsAsync(string key, CancellationToken ct = default);
}

