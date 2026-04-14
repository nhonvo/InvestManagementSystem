# Caching Strategy

> How InventoryAlert uses Redis to reduce external API calls, prevent duplicate processing, and enforce per-symbol cooldowns.

## Cache Layers

| Layer | Technology | Scope |
|---|---|---|
| **Quote Cache** | Redis (`StringGetAsync` / `StringSetAsync`) | Shared across all API and Worker instances |
| **Dedup Cache** | Redis (`StringSetAsync` NX) | SQS message deduplication |
| **Alert Cooldown** | Redis (`KeyExistsAsync`) | Per-symbol, 24h cooldown window |
| **Rate Limit Guard** | Redis (sliding window counter) | Finnhub call cap (55 rpm) |

---

## What Is Cached and Why

### 1. Stock Price Quotes (Redis, 30s TTL)

**Key**: `quote:{symbol}`

**Why**: Finnhub enforces strict rate limits (~60 req/min on free tier). When `SyncPricesJob` processes 50+ tickers, without caching each ticker would hit Finnhub on every run. With the 30-second cache, rapid successive requests (e.g., from UI + Worker simultaneously) serve from Redis.

```csharp
// StockDataService.cs
var cacheKey = $"quote:{symbol}";
var cached = await _cache.StringGetAsync(cacheKey);
if (cached.HasValue)
    return JsonSerializer.Deserialize<StockQuoteResponse>((string)cached!, _json);

var q = await _finnhub.GetQuoteAsync(symbol, ct);
await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(res, _json), TimeSpan.FromSeconds(30));
```

### 2. SQS Message Deduplication (Redis, 30min TTL)

**Key**: `dedup:sqs:{messageId}`

**Why**: `ProcessQueueJob` uses Redis atomic `SET NX` to guarantee each SQS message is processed exactly once, even if SQS delivers it multiple times (at-least-once delivery guarantee).

```csharp
// ProcessQueueJob.cs
var dedupKey = $"dedup:sqs:{envelope.MessageId}";
if (!await _redisDb.StringSetAsync(dedupKey, "1", TimeSpan.FromMinutes(30), When.NotExists))
    return; // Already processed — skip
```

### 3. Alert Cooldown (Redis, 24h TTL)

**Key**: `cooldown:alert:{symbol}`

**Why**: Prevents the same symbol's price breach from flooding users with repeated notifications within a short window. `PriceAlertHandler` checks this key before writing a `Notification` row.

```csharp
// PriceAlertHandler.cs
var alertKey = $"cooldown:alert:{payload.Symbol}";
if (await _redisDb.KeyExistsAsync(alertKey))
    return; // Suppressed within 24h window

await _redisDb.StringSetAsync(alertKey, "1", TimeSpan.FromHours(24));
```

### 4. Finnhub Rate Limit Guard (Redis, rolling 60s window)

**Key**: `finnhub:ratelimit`

**Why**: Free-tier Finnhub allows 60 requests/minute. The guard cap is set at **55 rpm** to leave a buffer. Any worker that exceeds this count skips the Finnhub call and uses local data instead.

---

## Cache Key Reference

| Key Pattern | TTL | Purpose |
|---|---|---|
| `quote:{symbol}` | **30 seconds** | Cached stock quote from Finnhub |
| `dedup:sqs:{messageId}` | 30 minutes | SQS message idempotency guard |
| `cooldown:alert:{symbol}` | 24 hours | Suppress repeated price alerts |
| `finnhub:ratelimit` | Rolling 60s | Sliding window counter — cap at 55 rpm |
| `search:{query}` | 4 hours | Search result cache |
| `peers:{symbol}` | 24 hours | Cached peer list |
| `metrics:{symbol}` | 1 hour | Cached basic financials |

---

## Why Not Cache in Memory?

The API and Worker run as **separate Docker containers**. In-process `IMemoryCache` is not shared between processes. Redis ensures a consistent cache that all containers in the stack see simultaneously.
