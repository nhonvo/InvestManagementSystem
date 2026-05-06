namespace InventoryAlert.Domain.Interfaces;

public interface IRedisHelper
{
    Task<bool> TryAcquireBestEffortLockAsync(string key, string value, TimeSpan expiry, CancellationToken ct = default);
    Task<bool> TryAcquireStrictLockAsync(string key, string value, TimeSpan expiry, CancellationToken ct = default);
    Task SetExpiryAsync(string key, TimeSpan expiry, CancellationToken ct = default);
    Task<bool> KeyExistsAsync(string key, CancellationToken ct = default);
    Task FlushDatabaseAsync(CancellationToken ct = default);
}

