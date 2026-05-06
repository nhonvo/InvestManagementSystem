using InventoryAlert.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace InventoryAlert.Infrastructure.Caching;

public class RedisHelper(IConnectionMultiplexer redis, ILogger<RedisHelper> logger) : IRedisHelper
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<bool> TryAcquireBestEffortLockAsync(string key, string value, TimeSpan expiry, CancellationToken ct = default)
    {
        try
        {
            return await _db.StringSetAsync(key, value, expiry, When.NotExists);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[RedisHelper] Error acquiring best-effort lock for {Key}", key);
            return true; // Fail-open
        }
    }

    public async Task<bool> TryAcquireStrictLockAsync(string key, string value, TimeSpan expiry, CancellationToken ct = default)
    {
        try
        {
            return await _db.StringSetAsync(key, value, expiry, When.NotExists);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[RedisHelper] Critical error acquiring strict lock for {Key}", key);
            return false; // Fail-closed
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

    public async Task FlushDatabaseAsync(CancellationToken ct = default)
    {
        var endpoints = redis.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            var server = redis.GetServer(endpoint);
            await server.FlushDatabaseAsync(_db.Database);
        }
    }
}
