# Caching Strategy

Last updated: **2026-05-01**

> How InventoryAlert uses Redis to reduce external API calls, provide idempotency for SQS processing, and enforce alert cooldowns.

## Cache use-cases

| Use-case | Where | Key pattern | TTL | Notes |
|---|---|---|---|---|
| Quote cache | API (`StockDataService`) | `quote:{symbol}` | 30s | Reduces Finnhub calls for hot symbols |
| Metrics cache | API (`StockDataService`) | `metrics:{symbol}` | 1h | Caches DB-sourced metrics response |
| Symbol search cache | API (`StockDataService`) | `search:{query}` | 4h | Caches Finnhub search results |
| SQS idempotency | Worker (`ProcessQueueJob`) | `msg:processed:{messageId}` | 24h | Written only after successful processing |
| Alert cooldown | Infra (`AlertRuleEvaluator`) | `inventoryalert:alerts:cooldown:v1:{userId}:{ruleId}` | 24h | Suppresses repeated notifications for the same rule |

## Notes and caveats

- The Worker currently relies on **bounded concurrency** (`WorkerSettings.MaxDegreeOfParallelism`) rather than a strict rate limiter for Finnhub.
- Cooldown is **per-user + per-rule**, not per-symbol.
- Some namespaces exist in `CacheKeys` (`InventoryAlert.Domain.Constants.CacheKeys`) but are not yet consistently used by all services (API quote/metrics/search still use simple keys).

