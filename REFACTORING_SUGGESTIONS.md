# Refactoring Suggestions for InventoryAlert

Last reviewed: **2026-04-30**

This document is a prioritized set of refactoring suggestions to improve maintainability, testability, and runtime performance for:

- `InventoryManagementSystem/InventoryAlert.Api`
- `InventoryManagementSystem/InventoryAlert.Worker`

It is based on the current source code shape (not generic advice). Where possible, each recommendation includes concrete targets (files/classes), a suggested approach, and “done” criteria.

## Quick prioritization

If you only do a few things, do these first:

1. Fix the **N+1 + sequential IO** in `PortfolioService.GetPositionsPagedAsync` (largest user-facing latency win).
2. Extract a small **Redis cache helper** (biggest maintainability win in `StockDataService`).
3. Introduce a consistent **application error contract** (reduce “mystery 500s” and improve client UX).

## Current observations (ground truth)

- `StockDataService` mixes **Finnhub calls + Redis caching + Postgres UoW + Dynamo news repos + symbol discovery**. (`InventoryManagementSystem/InventoryAlert.Api/Services/StockDataService.cs`)
- `PortfolioService.GetPositionsPagedAsync` loops and calls `GetPositionBySymbolAsync` per symbol, which performs multiple queries and an external quote call per item. (`InventoryManagementSystem/InventoryAlert.Api/Services/PortfolioService.cs`)
- `SyncPricesJob` already has some decoupling via `IAlertRuleEvaluator` + `IAlertNotifier`, but still runs a “fetch → evaluate → persist → notify” pipeline inside one job. (`InventoryManagementSystem/InventoryAlert.Worker/ScheduledJobs/SyncPricesJob.cs`)
- `IUnitOfWork` aggregates many repositories, increasing coupling across modules. (`InventoryManagementSystem/InventoryAlert.Domain/Interfaces/IUnitOfWork.cs`)
- The `.Result` mentioned previously in `StockDataService` is **not** a sync wait; it is a property access (`raw?.Result`) from Finnhub’s search response DTO. (`InventoryManagementSystem/InventoryAlert.Api/Services/StockDataService.cs`)

---

## 1) Architectural improvements

### 1.1 Decompose `StockDataService` (SRP boundary)

**Target**
- `InventoryManagementSystem/InventoryAlert.Api/Services/StockDataService.cs`
- Consumers: controllers + services using `IStockDataService` (e.g., `StocksController`, `MarketController`, `PortfolioService`, `WatchlistService`, `AlertRuleService`)

**Problem**
- The service owns too many axes of change (cache policy, Finnhub API mapping, local DB discovery, Dynamo news read models).
- This makes unit tests coarse-grained and encourages copy/paste caching patterns.

**Suggested split (incremental, low-risk)**
- Keep `IStockDataService` for now, but internally delegate to smaller components so you can migrate consumers later:
  - `ISymbolDiscoveryService` (DB lookup + Finnhub profile + persist listing)
  - `IQuoteService` (quote cache + Finnhub quote)
  - `IMarketCalendarService` (holidays, earnings/IPO calendars)
  - `INewsReadService` (market + company news via Dynamo repos)
  - `IIntelligenceReadService` (metrics/earnings/recommendations/insiders sourced from Postgres repos)

**Definition of done**
- `StockDataService` becomes a thin facade that composes smaller services (or is removed entirely in favor of those interfaces).
- Each component can be unit-tested with a single external dependency mocked (Finnhub OR Redis OR UoW).

**Notes**
- Avoid a “big bang rename”. Introduce delegates first, then gradually update controllers/services to depend on the smaller interfaces.

### 1.2 Modularize `IUnitOfWork` (reduce coupling)

**Target**
- `InventoryManagementSystem/InventoryAlert.Domain/Interfaces/IUnitOfWork.cs`

**Problem**
- One interface contains repositories for unrelated concerns (portfolio, market intelligence, notifications, etc.).
- This increases the blast radius: a service that needs `Trades` accidentally “sees” `Insiders`, `Recommendations`, etc.

**Options (choose one)**

**Option A — Split UoW by domain slice (recommended if you want strict boundaries)**
- `IPortfolioUnitOfWork`: Trades, WatchlistItems, StockListings (maybe), AlertRules (maybe)
- `IMarketIntelligenceUnitOfWork`: StockListings, Metrics, Earnings, Recommendations, Insiders, PriceHistories
- `INotificationUnitOfWork`: Notifications, AlertRules (read)

**Option B — Keep UoW, but narrow injection**
- Prefer injecting repositories directly into services unless a transaction is required.
- Reserve UoW injection for transactional operations only.

**Definition of done**
- New services do not inject the full `IUnitOfWork` by default.
- Transaction usage becomes explicit and localized.

### 1.3 Strengthen the `SyncPricesJob` pipeline boundaries (already partially done)

**Target**
- `InventoryManagementSystem/InventoryAlert.Worker/ScheduledJobs/SyncPricesJob.cs`

**What’s already good**
- Fetch is parallelized with `MaxDegreeOfParallelism`.
- Alert logic is delegated via `IAlertRuleEvaluator`.
- Delivery is delegated via `IAlertNotifier`.

**Remaining issues**
- Notifications are sent inline after DB operations; transient SignalR issues can slow the job.
- Persist and notify are tightly coupled; you can’t easily retry notification delivery independently.

**Suggested enhancement (incremental)**
- Persist notifications first, then publish delivery asynchronously:
  - Option A: enqueue a Hangfire background job per notification (or per batch) to call `_notifier.NotifyAsync`.
  - Option B: implement an Outbox table / delivery status and a separate delivery worker.

**Definition of done**
- Price sync succeeds even if SignalR delivery is temporarily failing.
- Notification delivery has dedicated retries/observability.

---

## 2) Performance optimizations

### 2.1 Fix the N+1 + sequential IO in `PortfolioService.GetPositionsPagedAsync`

**Target**
- `InventoryManagementSystem/InventoryAlert.Api/Services/PortfolioService.cs`

**Problem**
- `GetPositionsPagedAsync` loops `pagedItems` and calls `GetPositionBySymbolAsync` sequentially.
- Each call can trigger:
  - Trades fetch (`GetByUserAndSymbolAsync`)
  - Watchlist check (`GetByUserAndSymbolAsync`) when no trades exist
  - Listing fetch (`FindBySymbolAsync`)
  - Quote fetch (`IStockDataService.GetQuoteAsync`) which may hit Finnhub

**Suggested approach (two-step, safe)**
1. **Batch DB reads** for the paged symbols:
   - Trades for all symbols for user (single query)
   - Listings for all symbols (single query)
   - Watchlist items already loaded can be reused
2. **Batch quote reads**:
   - Add `GetQuotesAsync(IEnumerable<string>)` to `IStockDataService` (or a new quote service) that returns a dictionary.
   - Internally: use Redis multi-get + controlled Finnhub fan-out.

**Definition of done**
- `GetPositionsPagedAsync` performs O(1) DB round-trips per page (not per item).
- Quotes are fetched with bounded concurrency and cached consistently.

### 2.2 Extract and standardize caching policy (reduce duplication + mistakes)

**Target**
- `InventoryManagementSystem/InventoryAlert.Api/Services/StockDataService.cs`

**Problem**
- Cache read/deserialize/fallback/write is repeated across multiple methods with minor variations.

**Suggested approach**
- Introduce a small helper, e.g. `ICacheStore` or `RedisCacheClient` with:
  - `GetOrSetAsync<T>(key, ttl, factory)`
  - consistent JSON options (`JsonOptions.Default`)
  - optional “negative cache” for missing symbols (avoid hammering Finnhub on unknown tickers)

**Definition of done**
- Cache logic becomes one-liners; TTLs are declared next to the method behavior, not repeated mechanics.

### 2.3 Finnhub rate limiting + resilience

**Target**
- `InventoryManagementSystem/InventoryAlert.Worker/ScheduledJobs/SyncPricesJob.cs`
- `InventoryManagementSystem/InventoryAlert.Api/Services/StockDataService.cs`

**Current state**
- Worker uses `MaxDegreeOfParallelism` to reduce pressure, but that isn’t an actual rate limiter.

**Suggested approach**
- Implement a shared rate limiter around Finnhub calls:
  - Token bucket / fixed window limiter (per API key), shared per process.
  - Add retry/backoff for transient 429/5xx via Polly.

**Definition of done**
- Large symbol catalog does not cause sustained 429s and does not stall the worker.

---

## 3) Clean code & maintainability

### 3.1 Standardize business-rule errors (avoid `InvalidOperationException` for user-facing issues)

**Targets**
- `InventoryManagementSystem/InventoryAlert.Api/Services/PortfolioService.cs` (uses `InvalidOperationException` for expected domain rules)
- `InventoryManagementSystem/InventoryAlert.Api/Services/AuthService.cs` (uses `UserFriendlyException` with error codes)

**Problem**
- Mixing exception types makes it hard to guarantee consistent HTTP responses and error bodies.

**Suggested approach**
- Define a small set of app exceptions (or a Result type) that map to a stable API error contract:
  - `NotFound`, `Conflict`, `Validation`, `Unauthorized`, `Forbidden`, `UnprocessableEntity`
- Update service layer to throw those exceptions for expected conditions; reserve `InvalidOperationException` for programmer errors.

**Definition of done**
- API clients can reliably parse `errorCode` / `userFriendlyMessage` and render consistent UX.

### 3.2 Centralize cache keys and TTLs

**Target**
- `InventoryManagementSystem/InventoryAlert.Api/Services/StockDataService.cs` (keys like `quote:{symbol}`, `metrics:{symbol}`, `search:{query}`)

**Suggested approach**
- Create `CacheKeys` (and optionally `CacheTtls`) in Domain or Api constants:
  - `CacheKeys.Quote(symbol)`
  - `CacheKeys.Metrics(symbol)`
  - `CacheKeys.SymbolSearch(query)`

**Definition of done**
- No “magic key strings” remain in business services.

### 3.3 Encapsulate portfolio calculations

**Target**
- `InventoryManagementSystem/InventoryAlert.Api/Services/PortfolioService.cs` (net holdings, avg price, returns)

**Suggested approach**
- Extract calculation logic to:
  - a domain service `IPortfolioCalculator`, or
  - a pure helper in Domain layer (so it’s testable without DB/IO)

**Definition of done**
- Portfolio math has dedicated unit tests and no longer mixes with DB/HTTP/cache concerns.

---

## Proposed “refactor milestones” (optional)

1. **Performance pass (1–2 days)**: Batch reads in `PortfolioService` + add quote batching.
2. **Maintainability pass (1–2 days)**: Cache helper + centralized keys/TTLs.
3. **Reliability pass (2–4 days)**: Finnhub rate limiter + SyncPrices pipeline hardening.
4. **Architecture pass (ongoing)**: Decompose `StockDataService` + narrow `IUnitOfWork` usage.
