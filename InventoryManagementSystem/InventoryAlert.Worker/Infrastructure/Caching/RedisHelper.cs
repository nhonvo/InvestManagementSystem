using InventoryAlert.Worker.Interfaces;
using StackExchange.Redis;

namespace InventoryAlert.Worker.Infrastructure.Caching;

public class RedisHelper(IConnectionMultiplexer redis, ILogger<RedisHelper> logger) : IRedisHelper
{
    private readonly IDatabase _db = redis.GetDatabase();
    public async Task<bool> TryAcquireLockAsync(string key, string value, TimeSpan expiry, CancellationToken ct = default)
    {
        try
        {
            return await _db.StringSetAsync(key, value, expiry, When.NotExists);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Redis error while acquiring lock for key {Key}", key);
            return true; // Default to true so we don't accidentally skip processing on Redis failure
        }
    }
    public async Task SetExpiryAsync(string key, TimeSpan expiry, CancellationToken ct = default)
    {
        try
        {
            await _db.KeyExpireAsync(key, expiry);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to set expiry for key {Key}", key);
        }
    }
    public async Task<bool> KeyExistsAsync(string key, CancellationToken ct = default)
    {
        return await _db.KeyExistsAsync(key);
    }
}
