---
description: InventoryAlert Redis usage inventory plus recommended standards (keys, TTLs, error handling, and patterns).
type: reference
status: draft
version: 1.0
tags: [redis, caching, idempotency, cooldown, signalr, inventoryalert, standards]
last_updated: 2026-04-28
---

# Redis Standard & Usage (InventoryAlert)

This doc scans current Redis usage in the solution and proposes a standard approach for:

- what Redis is used for,
- key naming conventions,
- TTL policy,
- serialization,
- error-handling and operability.

Scope: `InventoryManagementSystem/*` only.

---

## 1) Where Redis is used today (observed)

### 1.1 API caching (`InventoryAlert.Api`)

File: `InventoryManagementSystem/InventoryAlert.Api/Services/StockDataService.cs`

Current keys:

- `quote:{symbol}` (TTL ~30s)
- `metrics:{symbol}` (TTL ~1h)
- `peers:{symbol}` (TTL ~1d)
- `search:{query}` (TTL ~4h)

Notes:

- Key prefixes here are “feature-based” but not namespaced by service (`api:`) or version (`v1:`).
- `InventoryManagementSystem/InventoryAlert.Domain/Constants/AlertConstants.cs` also defines `product:quote:{symbol}` which is currently **not used** by `StockDataService` (naming drift risk).

### 1.2 Alert cooldown/dedup (`InventoryAlert.Infrastructure`)

File: `InventoryManagementSystem/InventoryAlert.Infrastructure/Utilities/AlertRuleEvaluator.cs`

Key pattern:

- `alert:cooldown:{userId}:{ruleId}` (TTL ~24h)

Pattern:

- `KeyExists` check → if breached → `TryAcquireLockAsync(..., When.NotExists)` to set cooldown.

### 1.3 Worker message idempotency (`InventoryAlert.Worker`)

File: `InventoryManagementSystem/InventoryAlert.Worker/ScheduledJobs/ProcessQueueJob.cs`

Key pattern:

- `msg:processed:{messageId}` (TTL ~24h)

Pattern:

- Check `KeyExists` → process envelope → on success set `StringSetAsync` with TTL.

### 1.4 SignalR backplane (API + Worker)

Files:

- `InventoryManagementSystem/InventoryAlert.Api/Program.cs`
- `InventoryManagementSystem/InventoryAlert.Worker/Program.cs`

Observed:

- `options.Configuration.ChannelPrefix = "InventoryAlert_SignalR"`

This is correct (prevents cross-app collisions when sharing a Redis instance).

### 1.5 Redis abstraction helper

File: `InventoryManagementSystem/InventoryAlert.Infrastructure/Caching/RedisHelper.cs`

Methods:

- `TryAcquireLockAsync(key, value, expiry)` uses `StringSetAsync(..., When.NotExists)`
- `SetExpiryAsync(key, expiry)` uses `KeyExpireAsync`
- `KeyExistsAsync(key)`

Important behavior:

- On Redis exception inside `TryAcquireLockAsync`, it returns `true` (fail-open) to avoid accidentally skipping processing.

---

## 2) Standard Redis responsibilities (recommended)

### 2.1 Allowed uses

Use Redis for:

- short-lived caches (quote/metrics/search)
- throttling/cooldown and alert storm prevention
- message idempotency markers (`processed`)
- distributed locks (short TTL) for “only one worker does X”
- SignalR backplane

Avoid Redis for:

- permanent state (use Postgres/Dynamo)
- long-term audit logs (use Seq + DB/Dynamo)
- storing secrets/tokens/PII

### 2.2 Ownership rule

Prefer one of these patterns:

1) API cache keys are owned by API services only.
2) Worker idempotency keys are owned by Worker only.
3) Shared key contracts must live in `InventoryAlert.Domain` as constants and be used everywhere.

Do not mix “ad-hoc strings” and “domain constants” for the same concept (prevents drift like `quote:{symbol}` vs `product:quote:{symbol}`).

---

## 3) Key naming standard (recommended)

### 3.1 Base format

Use a consistent prefix:

`{product}:{service}:{purpose}:{version}:{...ids}`

Examples:

- `inventoryalert:api:quote:v1:{SYMBOL}`
- `inventoryalert:api:search:v1:{QUERY_HASH}`
- `inventoryalert:worker:msg-processed:v1:{MESSAGE_ID}`
- `inventoryalert:alerts:cooldown:v1:{USER_ID}:{RULE_ID}`

Rules:

- Always use lowercase for static segments.
- Use uppercase for symbols (`AAPL`) and normalize before building keys.
- Prefer hashed keys for high-cardinality unbounded strings:
  - `search:{sha256(query)}` rather than raw query text.

### 3.2 TTL encoded in purpose (optional)

If TTL expectations are strict, encode them:

- `quote30s`, `metrics1h`, `cooldown24h`

Example:

- `inventoryalert:api:quote30s:v1:AAPL`

---

## 4) TTL policy (recommended)

### 4.1 Caches

- Quote: 15–60s
- Metrics: 30–120m
- Search: 1–12h (prefer hash key)
- Peers: 1–7d

### 4.2 Cooldowns / idempotency

- Alert cooldown: 1–24h (based on desired “spam control”)
- Message processed marker: 24–72h (based on max replay window and DLQ redrive behavior)

### 4.3 Expiration is mandatory

No key should be written without expiry unless it is:

- a SignalR backplane internal artifact, or
- a deliberate long-lived lock with separate maintenance (rare; usually avoid).

---

## 5) Serialization & value guidelines (recommended)

- Use `JsonOptions.Default` consistently across API/Worker for JSON payloads.
- Cache values should be:
  - small and bounded (avoid logging them; cache bodies can be big)
  - versioned DTOs (breaking changes handled by key version bump)

---

## 6) Error handling (recommended)

### 6.1 Avoid “silent” correctness changes

Today `RedisHelper.TryAcquireLockAsync` returns `true` on Redis failures (fail-open).

This is acceptable for:

- cooldown locks (better to allow alert spam than stop the system),
- best-effort throttles.

But it is risky for:

- strong idempotency (may cause duplicate processing),
- financial side effects if handlers aren’t idempotent.

Recommendation:

- Split helpers by intent:
  - `TryAcquireBestEffortLockAsync` (fail-open)
  - `TryAcquireStrictLockAsync` (fail-closed + alert/metrics)

### 6.2 Add minimal operability hooks

Standard log fields when Redis fails:

- `CorrelationId` (if present)
- `RedisKeyPrefix` (not full key if it contains user ids)
- `Operation` (`StringSetNX`, `KeyExpire`, `StringGet`)

---

## 7) Concrete improvement checklist (next refactor)

- Consolidate key naming:
  - choose either `quote:{symbol}` or `product:quote:{symbol}` and remove the other; prefer domain constant if multiple components rely on it.
- Replace raw `search:{query}` with a hashed key to prevent massive key length / leakage.
- Add versioned prefixes for all keys to avoid stale cache after DTO changes.
- Decide idempotency window for `msg:processed:{messageId}` relative to DLQ/redrive rules.

