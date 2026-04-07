namespace InventoryAlert.Worker.Interfaces;

public interface IRedisHelper
{
    /// <summary>Attempts to acquire a distributed lock (SET NX).</summary>
    Task<bool> TryAcquireLockAsync(string key, string value, TimeSpan expiry, CancellationToken ct = default);
    /// <summary>Extends the TTL of a key (e.g., after success).</summary>
    Task SetExpiryAsync(string key, TimeSpan expiry, CancellationToken ct = default);
    /// <summary>Check if a key exists.</summary>
    Task<bool> KeyExistsAsync(string key, CancellationToken ct = default);
}
