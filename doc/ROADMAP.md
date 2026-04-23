---
description: Project roadmap (planned work, milestones, and priorities).
type: plan
status: active
version: 1.0
tags: [roadmap, planning, inventoryalert]
last_updated: 2026-04-23
---

# 🗺️ Development Roadmap

> Consolidated from all `TODO` comments across the codebase.
> Each task preserves the original TODO content and is grouped by theme, ordered by priority.

---

## 🔴 P0 — Critical / Blocking (Do First)

### 1. Docker & Containerization ✅

> **Source:** `ProductController.cs:L96` — _important high priority_

~~Setup docker and containerization for the application.~~ **✅ Complete.** See `SETUP_COMMANDS.md` and `EVENT_DRIVEN_PLAN.md` for full implementation details.

- [x] Create `Dockerfile` (multi-stage build)
- [x] Create `docker-compose.yml` (api + postgres + redis + moto + moto-init + worker)
- [x] Externalize config via `appsettings.Docker.json` (not inline env vars)
- [x] Moto init script (`SolutionFolder/moto-init/init-sqs.sh`) auto-creates SNS/SQS on boot

### 2. Global Exception Handling & Error Responses

> **Source:** `ProductController.cs:L79,L89,L91`

Implement a global exception handling mechanism (middleware or filters) to catch unhandled exceptions and return consistent, user-friendly JSON error responses. Use custom exceptions (`NotFoundException`, `ValidationException`) to represent different error types. Return appropriate HTTP status codes (400, 404, 500) with error codes. Ensure sensitive information is not exposed. Keep it simple and easy to understand for junior developers.

- [x] Create custom exception classes (`NotFoundException`, `ValidationException`)
- [x] Implement `ExceptionHandlingMiddleware`
- [x] Standardize error response format (`{ code, message, details }`)

### 3. Logging Strategy

> **Source:** `ProductController.cs:L81,L91`

Implement comprehensive logging using Serilog or NLog. Log important events (product creation, updates, deletions) and errors with appropriate log levels (Information, Warning, Error). Include contextual information (product ID, user ID). Configure multiple sinks (console, file, database).

> **Note:** `FinnhubSyncWorker` logging should be addressed here but the worker itself is planned for **retirement** once `SyncPricesJob` in `InventoryAlert.Worker` is live (see `EVENT_DRIVEN_PLAN.md` — Phase C).

- [x] Install and configure Serilog (Api + Worker)
- [x] Add structured logging to `ProductService` via `ILogger<T>`
- [x] `CorrelationIdMiddleware` — stamps all requests and logs with `X-Correlation-Id`
- [x] `FinnhubSyncWorker` retired — `SyncPricesJob` in Worker owns price sync

---

## 🟠 P1 — Architecture & Code Quality

### 4. Thin Controller / Service Layer Refactoring

> **Source:** `ProductController.cs:L59`

All the logic should be in the service layer. Move remaining logic to service layer and make the controller thin and simple, just for handling HTTP request and response.

**Current violation — `UpdateProductStockCount` (L49-L57):**
The PATCH endpoint fetches the product, mutates it, then calls update — this logic belongs in the service.

- [x] Create `UpdateStockCountAsync(int id, int stockCount)` in `IProductService`
- [x] Move PATCH logic from controller to service
- [x] Controller should only call service and return result

### 5. RESTful API Design Review

> **Source:** `ProductController.cs:L30,L49,L72,L85,L86,L87,L88`

Review the design of the API endpoints to follow RESTful principles more closely:

| Current Issue                                             | Fix                                         |
| --------------------------------------------------------- | ------------------------------------------- |
| `HttpGet("{id}")` uses `[FromQuery]` but route has `{id}` | Use `[FromRoute]` or remove route param     |
| `HttpPatch` has no route param for ID                     | Change to `HttpPatch("{id}")`               |
| `HttpDelete` has no route param for ID                    | Change to `HttpDelete("{id}")`              |
| `CreateProduct` returns `200 OK`                          | Return `201 Created`                        |
| `GetProductsByIds` name is misleading (single ID)         | Rename to `GetProductById`                  |
| Method/endpoint naming inconsistency                      | Use verbs for actions + nouns for resources |

- [x] Fix route parameter binding — all endpoints use `[FromRoute]`
- [x] Return correct HTTP status codes (`201 Created`, `204 NoContent`, `404 NotFound`)
- [x] Rename methods — `GetProductById`, `UpdateStockCount`, `GetPriceLossAlerts`
- [x] Kept entity name as `Product` — domain language matches business context (no rename needed)

### 6. Unit of Work Pattern Review

> **Source:** `UnitOfWork.cs:L5`, `ProductServices.cs:L62`

Review the UnitOfWork pattern. Current concern: "I just want to reach rollback when scaling up with more tables. If fail when inserting one table, rollback the whole thing. Should we keep UnitOfWork or use a simpler implementation?"

**Verdict:** Keep UnitOfWork. It provides transaction safety for multi-table operations. For single-table operations, it adds minimal overhead since EF Core's `SaveChanges` is already atomic.

- [x] Audit all service methods for consistent UnitOfWork usage
- [x] Ensure `ExecuteTransactionAsync` wraps only multi-step operations
- [x] Document when to use `UnitOfWork` vs plain `SaveChangesAsync`

### 7. Program.cs Organization

> **Source:** `Program.cs:L15,L34`

Review and enhance the `Program.cs` design. Use extension methods to organize the code and make it more readable and maintainable. Move static file names to constants.

- [x] Create `ServiceCollectionExtensions.AddInfrastructure()`
- [x] Create `ServiceCollectionExtensions.AddApplication()`
- [x] Extract magic strings to a `Constants` class

### 8. Stock & Market Data Service Implementation ✅

> **Source:** USER Request — **Completed 2026-04-07**

Implement a comprehensive service for orchestrated stock, market, and crypto data fetching with multi-layer caching (Redis + Postgres).

- [x] Create `IStockDataService` and `StockDataService`
- [x] Implement Redis-first caching for Quotes, Peers, Market Status
- [x] Implement DB-first caching for Company Profiles
- [x] Full coverage for News, Recommendations, Earnings, and Calendars
- [x] Crypto exchange and symbol support

---

## 🟡 P2 — Data Integrity & Validation

### 8. Input Validation

> **Source:** `ProductController.cs:L71,L87`

Add validation for input data using FluentValidation or Data Annotations. Ensure data being processed is valid and provide meaningful error messages when validation fails. Review use of data annotations or validation attributes on DTOs.

- [x] Install `FluentValidation.AspNetCore`
- [x] Create `ProductRequestDtoValidator`
- [x] Register validators in DI
- [x] Return `400 Bad Request` with validation details

### 9. DTO Design Review

> **Source:** `ProductController.cs:L74,L76`, `ProductServices.cs:L40`

Review `ProductDto` and `ProductRequestDto`. Separate the properties required for creating vs updating. Rename `GetHighValueProducts` to something more descriptive like `GetSignificantLossProducts`. The update method should use the ID from the route, not the body.

**Current state:** `ProductRequestDto` (create/update) and `ProductDto` (response) are already separated. ✅

- [x] `UpdateProductAsync` always uses route ID (body ID ignored)
- [x] Renamed `GetHighValueProducts` → `GetPriceLossAlertsAsync`
- [x] Renamed endpoint `low-stock` → `price-alerts`

### 10. Null Handling in Finnhub Responses

> **Source:** `ProductServices.cs:L107`

Should we check if the price is null before assigning it to `CurrentPrice`? Or should we set the current price to 0 if the price is null?

**Recommendation:** Skip the product entirely if the price is null. Setting it to 0 would trigger false loss alerts.

- [x] `quote?.CurrentPrice is null or 0` → `continue` when null (ProductService + SyncPricesJob)
- [x] Warning logged when a symbol returns null or zero price

### 11. BulkInsert Response Design

> **Source:** `ProductServices.cs:L71`

Should we return the list of created products or just the count?

**Recommendation:** Return `204 No Content` or the count. Returning thousands of full objects wastes bandwidth.

- [x] `BulkInsertProducts` returns `204 No Content`
- [x] No body returned — bandwidth optimized

---

## 🏗️ P2 — Modernization & UI (New)

### 12. Next.js 15 Web Dashboard ✅
 
 > **Source:** USER Request — **Completed 2026-04-07**
 
 - [x] Create `InventoryAlert.UI` project (Next.js 15 + Tailwind CSS)
 - [x] Implement modern dark-themed UI with glassmorphism
 - [x] Routes: Market Explorer, Price Alerts, Portfolio, Symbol Details
 - [x] Integrated with `StocksController` via `StockDataService`
 - [x] **UI Test stabilization** — Added Vitest unit and component tests to satisfy CI/CD
 - [ ] Replace static chart placeholders with functional **Recharts** implementations
 - [ ] Implement Next.js Route Handlers for secure API proxying


### 13. Shared Project — `InventoryAlert.Domain` ✅ (Already Exists)

> **Note:** `InventoryAlert.Domain` **already exists** as the shared project.
> Both `InventoryAlert.Api` and `InventoryAlert.Worker` reference it via `<ProjectReference>` — **no new project needed**.
> The tasks below are **enhancements** to the existing shared project.
>
> **Invariant:** `InventoryAlert.Domain` is the single source of truth for every type that crosses a service boundary. No entity or payload may be defined in both `Api` and `Worker`.

#### What the Shared Project Must Own

| Category                                      | Current State                                        | Target                             |
| :-------------------------------------------- | :--------------------------------------------------- | :--------------------------------- |
| Domain entities (`Product`, `EventLog`, …)    | ✅ In `Contracts/Entities/`                          | Keep — verified                    |
| Event payloads (`MarketPriceAlertPayload`, …) | ✅ In `Contracts/Events/Payloads/`                   | Keep — verified                    |
| `EventEnvelope`                               | ✅ Updated — `EventType`, `Payload`, `CorrelationId` | Keep                               |
| `EventTypes` constants                        | ✅ Reverse-DNS naming applied                        | Keep                               |
| App-wide constants (cache keys, SQS headers)  | ✅ In `Contracts/Constants/`                         | Keep                               |
| Shared configuration models                   | ❌ Duplicated per project                            | Move to `Contracts/Configuration/` |

#### Build Tasks

- [x] Entities centralized: `Product`, `EventLog`, `EarningsRecord`, `InsiderTransaction`, `NewsRecord`
- [x] `global using` aliases in `Api/Domain/Entities/SharedEntityAliases.cs` for backward compat
- [x] `global using` aliases in `Worker/SharedEntityAliases.cs` for backward compat
- [x] `Constants/AlertConstants.cs` — `CacheKeys`, `EventTypes`, `SqsHeaders` centralized
- [x] `Events/EventEnvelope.cs` — standardized envelope with `CorrelationId` + `Source`
- [x] `Events/EventTypes.cs` — reverse-DNS format + `IsKnown()` + `IReadOnlySet<string>`
- [x] **Move shared config models** — create `Contracts/Configuration/SharedAwsSettings.cs` and `SharedFinnhubSettings.cs` so both `Api` and `Worker` read from the same schema
- [x] **Add `InventoryAlert.ArchitectureTests`** — use `NetArchTest.Rules` to **enforce** at CI time that:
  - `InventoryAlert.Api` has no internal entity definitions
  - `InventoryAlert.Worker` has no internal entity definitions
  - All event payloads live only in `Contracts`
- [x] **NuGet packaging (future)** — if a 3rd service (e.g. `InventoryAlert.Sample`) is added, package `Contracts` as a private NuGet feed artifact so versioning is explicit

#### Reference Architecture

```text
InventoryAlert.Domain/  ← shared library, referenced by all
├── Entities/              ← EF-mapped domain entities
├── Events/
│   ├── EventEnvelope.cs   ← envelope contract
│   ├── EventTypes.cs      ← canonical type registry
│   └── Payloads/          ← one file per event type
├── Constants/
│   └── AlertConstants.cs  ← CacheKeys, SqsHeaders, defaults
└── Configuration/         ← PLANNED: shared config POCOs
    ├── SharedAwsSettings.cs
    └── SharedFinnhubSettings.cs

InventoryAlert.Api/
  └── Domain/Entities/SharedEntityAliases.cs  ← global using only

InventoryAlert.Worker/
  └── SharedEntityAliases.cs                  ← global using only
```

---

## 📂 P3 — Logging & Observability (New)

### 14. Centralized Logging (Serilog) ✅

> **Source:** USER Request — **Completed 2026-04-05**

- [x] Serilog bootstrapped in `InventoryAlert.Worker/Program.cs`
- [x] JSON file sink + Console sink (rolling daily, 7-day retention)
- [x] `CorrelationIdMiddleware` tracks requests across Api and Worker
- [x] `MachineName` and `EnvironmentName` enrichers added

### 15. Global Exception Strategy ✅

> **Source:** USER Request — **Completed 2026-04-05**

- [x] `ExceptionHandlingMiddleware` refactored for full RFC-7807 compliance (`type`, `instance`, semantic slugs)
- [x] `HangfireJobLoggingFilter` — global job error auditing via `ILogger`
- [x] All domain exceptions (`NotFoundException`, `ValidationException`, `ArgumentException`) mapped to HTTP codes

---

## 🟢 P4 — Performance & Scalability

### 12. Caching ✅

> **Source:** `ProductController.cs:L68`

Implement caching for frequently accessed data (product details) using in-memory caching or Redis, depending on scale and expected load.

- [x] Add `IMemoryCache` for `GetProductById`
- [x] Add cache invalidation on Update/Delete
- [x] Evaluate Redis if scaling beyond a single instance

### 13. Pagination ✅

> **Source:** `ProductController.cs:L73`

Implement pagination for list endpoints (`GetProducts`). Accept query parameters for page number and page size. Return subset of data with metadata (total items, total pages).

- [x] Create `PaginationParams` (PageNumber, PageSize)
- [x] Create `PagedResult<T>` wrapper
- [x] Update `GetAllAsync` in repository to support `Skip/Take`

### 14. Finnhub Sync Design Review
**Verdict:** Keep UnitOfWork. It provides transaction safety for multi-table operations. For single-table operations, it adds minimal overhead since EF Core's `SaveChanges` is already atomic.

- [x] Audit all service methods for consistent UnitOfWork usage
- [x] Ensure `ExecuteTransactionAsync` wraps only multi-step operations
- [x] Document when to use `UnitOfWork` vs plain `SaveChangesAsync`

### 7. Program.cs Organization

> **Source:** `Program.cs:L15,L34`

Review and enhance the `Program.cs` design. Use extension methods to organize the code and make it more readable and maintainable. Move static file names to constants.

- [x] Create `ServiceCollectionExtensions.AddInfrastructure()`
- [x] Create `ServiceCollectionExtensions.AddApplication()`
- [x] Extract magic strings to a `Constants` class

### 8. Stock & Market Data Service Implementation ✅

> **Source:** USER Request — **Completed 2026-04-07**

Implement a comprehensive service for orchestrated stock, market, and crypto data fetching with multi-layer caching (Redis + Postgres).

- [x] Create `IStockDataService` and `StockDataService`
- [x] Implement Redis-first caching for Quotes, Peers, Market Status
- [x] Implement DB-first caching for Company Profiles
- [x] Full coverage for News, Recommendations, Earnings, and Calendars
- [x] Crypto exchange and symbol support

---

## 🟡 P2 — Data Integrity & Validation

### 8. Input Validation

> **Source:** `ProductController.cs:L71,L87`

Add validation for input data using FluentValidation or Data Annotations. Ensure data being processed is valid and provide meaningful error messages when validation fails. Review use of data annotations or validation attributes on DTOs.

- [x] Install `FluentValidation.AspNetCore`
- [x] Create `ProductRequestDtoValidator`
- [x] Register validators in DI
- [x] Return `400 Bad Request` with validation details

### 9. DTO Design Review

> **Source:** `ProductController.cs:L74,L76`, `ProductServices.cs:L40`

Review `ProductDto` and `ProductRequestDto`. Separate the properties required for creating vs updating. Rename `GetHighValueProducts` to something more descriptive like `GetSignificantLossProducts`. The update method should use the ID from the route, not the body.

**Current state:** `ProductRequestDto` (create/update) and `ProductDto` (response) are already separated. ✅

- [x] `UpdateProductAsync` always uses route ID (body ID ignored)
- [x] Renamed `GetHighValueProducts` → `GetPriceLossAlertsAsync`
- [x] Renamed endpoint `low-stock` → `price-alerts`

### 10. Null Handling in Finnhub Responses

> **Source:** `ProductServices.cs:L107`

Should we check if the price is null before assigning it to `CurrentPrice`? Or should we set the current price to 0 if the price is null?

**Recommendation:** Skip the product entirely if the price is null. Setting it to 0 would trigger false loss alerts.

- [x] `quote?.CurrentPrice is null or 0` → `continue` when null (ProductService + SyncPricesJob)
- [x] Warning logged when a symbol returns null or zero price

### 11. BulkInsert Response Design

> **Source:** `ProductServices.cs:L71`

Should we return the list of created products or just the count?

**Recommendation:** Return `204 No Content` or the count. Returning thousands of full objects wastes bandwidth.

- [x] `BulkInsertProducts` returns `204 No Content`
- [x] No body returned — bandwidth optimized

---

## 🏗️ P2 — Modernization & UI (New)

### 12. Next.js 15 Web Dashboard ✅
 
 > **Source:** USER Request — **Completed 2026-04-07**
 
 - [x] Create `InventoryAlert.UI` project (Next.js 15 + Tailwind CSS)
 - [x] Implement modern dark-themed UI with glassmorphism
 - [x] Routes: Market Explorer, Price Alerts, Portfolio, Symbol Details
 - [x] Integrated with `StocksController` via `StockDataService`
 - [ ] Replace static chart placeholders with functional **Recharts** implementations
 - [ ] Implement Next.js Route Handlers for secure API proxying


### 13. Shared Project — `InventoryAlert.Domain` ✅ (Already Exists)

> **Note:** `InventoryAlert.Domain` **already exists** as the shared project.
> Both `InventoryAlert.Api` and `InventoryAlert.Worker` reference it via `<ProjectReference>` — **no new project needed**.
> The tasks below are **enhancements** to the existing shared project.
>
> **Invariant:** `InventoryAlert.Domain` is the single source of truth for every type that crosses a service boundary. No entity or payload may be defined in both `Api` and `Worker`.

#### What the Shared Project Must Own

| Category                                      | Current State                                        | Target                             |
| :-------------------------------------------- | :--------------------------------------------------- | :--------------------------------- |
| Domain entities (`Product`, `EventLog`, …)    | ✅ In `Contracts/Entities/`                          | Keep — verified                    |
| Event payloads (`MarketPriceAlertPayload`, …) | ✅ In `Contracts/Events/Payloads/`                   | Keep — verified                    |
| `EventEnvelope`                               | ✅ Updated — `EventType`, `Payload`, `CorrelationId` | Keep                               |
| `EventTypes` constants                        | ✅ Reverse-DNS naming applied                        | Keep                               |
| App-wide constants (cache keys, SQS headers)  | ✅ In `Contracts/Constants/`                         | Keep                               |
| Shared configuration models                   | ❌ Duplicated per project                            | Move to `Contracts/Configuration/` |

#### Build Tasks

- [x] Entities centralized: `Product`, `EventLog`, `EarningsRecord`, `InsiderTransaction`, `NewsRecord`
- [x] `global using` aliases in `Api/Domain/Entities/SharedEntityAliases.cs` for backward compat
- [x] `global using` aliases in `Worker/SharedEntityAliases.cs` for backward compat
- [x] `Constants/AlertConstants.cs` — `CacheKeys`, `EventTypes`, `SqsHeaders` centralized
- [x] `Events/EventEnvelope.cs` — standardized envelope with `CorrelationId` + `Source`
- [x] `Events/EventTypes.cs` — reverse-DNS format + `IsKnown()` + `IReadOnlySet<string>`
- [x] **Move shared config models** — create `Contracts/Configuration/SharedAwsSettings.cs` and `SharedFinnhubSettings.cs` so both `Api` and `Worker` read from the same schema
- [x] **Add `InventoryAlert.ArchitectureTests`** — use `NetArchTest.Rules` to **enforce** at CI time that:
  - `InventoryAlert.Api` has no internal entity definitions
  - `InventoryAlert.Worker` has no internal entity definitions
  - All event payloads live only in `Contracts`
- [x] **NuGet packaging (future)** — if a 3rd service (e.g. `InventoryAlert.Sample`) is added, package `Contracts` as a private NuGet feed artifact so versioning is explicit

#### Reference Architecture

```text
InventoryAlert.Domain/  ← shared library, referenced by all
├── Entities/              ← EF-mapped domain entities
├── Events/
│   ├── EventEnvelope.cs   ← envelope contract
│   ├── EventTypes.cs      ← canonical type registry
│   └── Payloads/          ← one file per event type
├── Constants/
│   └── AlertConstants.cs  ← CacheKeys, SqsHeaders, defaults
└── Configuration/         ← PLANNED: shared config POCOs
    ├── SharedAwsSettings.cs
    └── SharedFinnhubSettings.cs

InventoryAlert.Api/
  └── Domain/Entities/SharedEntityAliases.cs  ← global using only

InventoryAlert.Worker/
  └── SharedEntityAliases.cs                  ← global using only
```

---

## 📂 P3 — Logging & Observability (New)

### 14. Centralized Logging (Serilog) ✅

> **Source:** USER Request — **Completed 2026-04-05**

- [x] Serilog bootstrapped in `InventoryAlert.Worker/Program.cs`
- [x] JSON file sink + Console sink (rolling daily, 7-day retention)
- [x] `CorrelationIdMiddleware` tracks requests across Api and Worker
- [x] `MachineName` and `EnvironmentName` enrichers added

### 15. Global Exception Strategy ✅

> **Source:** USER Request — **Completed 2026-04-05**

- [x] `ExceptionHandlingMiddleware` refactored for full RFC-7807 compliance (`type`, `instance`, semantic slugs)
- [x] `HangfireJobLoggingFilter` — global job error auditing via `ILogger`
- [x] All domain exceptions (`NotFoundException`, `ValidationException`, `ArgumentException`) mapped to HTTP codes

---

## 🟢 P4 — Performance & Scalability

### 12. Caching ✅

> **Source:** `ProductController.cs:L68`

Implement caching for frequently accessed data (product details) using in-memory caching or Redis, depending on scale and expected load.

- [x] Add `IMemoryCache` for `GetProductById`
- [x] Add cache invalidation on Update/Delete
- [x] Evaluate Redis if scaling beyond a single instance

### 13. Pagination ✅

> **Source:** `ProductController.cs:L73`

Implement pagination for list endpoints (`GetProducts`). Accept query parameters for page number and page size. Return subset of data with metadata (total items, total pages).

- [x] Create `PaginationParams` (PageNumber, PageSize)
- [x] Create `PagedResult<T>` wrapper
- [x] Update `GetAllAsync` in repository to support `Skip/Take`

### 14. Finnhub Sync Design Review ✅

> **Source:** `ProductServices.cs:L89`

Should we get the quote for each product on every API call, or store the price in the database and update it regularly?

**Current state:** ✅ Already implemented! `SyncCurrentPricesAsync` updates DB via BackgroundWorker. `GetHighValueProducts` now reads from DB (no live API calls).

- [x] `GetHighValueProducts` reads only from DB for faster response
- [x] BackgroundWorker is the single source of truth for `CurrentPrice`

---

## 🔵 P4 — Security

### 15. Authentication & Authorization ✅

> **Source:** `ProductController.cs:L69`

Add simple JWT-based authentication to secure the API endpoints and ensure only authorized users can access certain resources.

- [x] Install `Microsoft.AspNetCore.Authentication.JwtBearer`
- [x] Configure JWT in `Program.cs`
- [x] Add `[Authorize]` to protected endpoints
- [x] Create login/token endpoint
- [x] **User Registration** — Implemented `POST /api/auth/register` with unique index, password hashing, and transaction safety.

### 16. Web Security (OWASP) ✅

> **Source:** `ProductController.cs:L84`

Protect against SQL injection, XSS, and CSRF. Use parameterized queries (EF Core handles this ✅), validate/sanitize input data, enforce HTTPS.

- [x] Enable HTTPS redirection (`app.UseHttpsRedirection()`)
- [x] Add CORS policy
- [x] Review EF Core queries for raw SQL (ensure parameterized)
- [x] Add rate limiting for Finnhub-related endpoints

---

## 🟣 P5 — Testing & Quality

### 17. Codebase Audit & Standardization ✅

> **Source:** USER Request — _Quality baseline. See `AUDIT_PLAN.md` for full spec._
>
> **Goal:** Ensure file names, methods, structure grids, folder patterns, and tech logic are strictly conformed across `Api`, `Worker` and `Contracts`.

- [x] Execute **Structure Audit** (Verify DDD isolation, clean out legacy entity files from Api Project).
- [x] Execute **Naming Audit** (Interfaces, Service suffixes, Exception suffixes, Job/Handler match).
- [x] Execute **Logic Audit** (Enforce thin controllers, centralized mapping, UnitOfWork transaction capture pattern).
- [x] Execute **Tech Pattern Audit** (Async/await hygiene, `AsNoTracking`, strictly `ILogger<T>` usage, `CancellationToken` cascading).

### 18. Test Project Standardization (Unit & Integration) ✅

> **Source:** USER Request — _Testing baseline definition._
>
> **Goal:** Strictly segregate test types and ensure all 3 main projects (`Api`, `Worker`, `Contracts`) are explicitly covered by testing.

- [x] Rename `InventoryAlert.Tests` to `InventoryAlert.UnitTests`.
- [x] Group unit tests logically inside `InventoryAlert.UnitTests`: structure them exactly to mirror the 3 target projects (`Api`, `Worker`, `Contracts`).
- [x] Ensure unit tests only use mocks (`Moq`) and don't hit real databases or brokers.
- [x] Create a new project `InventoryAlert.IntegrationTests` for end-to-end and DB testing (e.g., hitting Testcontainers or an EF Core InMemory DB, and Moto for AWS).
- [x] **107 Unit Tests Passing** — Verified full coverage for `ProductService`, `WatchlistService` (with Redis integration), and `AuthController`.

**Existing Unit Tests (`InventoryAlert.UnitTests`) progress:**

- [x] Test `ProductServices.GetHighValueProducts` (loss math)
- [x] Test `ProductServices.SyncCurrentPricesAsync` (null handling)
- [x] Test `ProductController` status codes (200, 201, 404)
- [x] Mock `IFinnhubClient` and `IProductRepository`
- [x] Test `EventService` (transaction call, publisher payload)
- [x] Test `EventsController` (202/400 statuses, payload mapping)
- [x] Test `WatchlistService` (caching, deduplication)
- [x] Test `AuthController` (Registration conflicts, async handlers)

### 19. E2E Test Stabilization ✅

> **Source:** USER Request — _Stabilization baseline._

- [x] Docker environment verified (Postgres + Redis + Moto + API)
- [x] `BaseE2ETest` improved with configurable `BaseUrl` and detailed error logging
- [x] 32 E2E tests verified passing against Docker-hosted API

### 20. Async/Await Audit ✅

> **Source:** `ProductController.cs:L70,L78`

Review all async/await usage. Ensure all operations are properly awaited. Review `CancellationToken` consistency across all layers.

- [x] Add `CancellationToken` to `DeleteProductAsync`
- [x] Ensure `CancellationToken` flows: Controller → Service → Repository
- [x] Remove `CancellationToken.None` usages where a real token is available

### 20. Dependency Injection Review ✅

> **Source:** `ProductController.cs:L92`

Review DI usage. Ensure constructor injection is used consistently. Review service lifetimes (transient, scoped, singleton) for correctness.

**Current state:**

| Service              | Lifetime              | Correct?                           |
| :------------------- | :-------------------- | :--------------------------------- |
| `RestClient`         | via HttpClientFactory | ✅                                 |
| `IFinnhubClient`     | Scoped                | ✅                                 |
| `IProductService`    | Scoped                | ✅                                 |
| `IProductRepository` | Scoped                | ✅                                 |
| `IConnectionMultiplier` | Singleton          | ✅ (Redis Multiplexer)              |
| `FinnhubSyncWorker`  | Removed               | ✅ Hosted via Hangfire instead     |

- [x] Verify `FinnhubSyncWorker` uses `IServiceScopeFactory` (not direct injection) - **Worker replaced entirely by Hangfire Job!**
- [x] Document DI registration decisions

---

## ⚫ P6 — DevOps & Automation

### 21. Messaging & Advanced Background Jobs ✅

> **Source:** `ProductController.cs:L97` — _important high priority_

See `EVENT_DRIVEN_PLAN.md` for full architecture. Infrastructure scaffolding is complete:

- [x] Evaluate Hangfire vs current `BackgroundService` → **Hangfire chosen**
- [x] Design message schema for price alerts → `EventEnvelope` + payload records in `Contracts`
- [x] `InventoryAlert.Worker` project created + NuGet packages installed
- [x] Docker moto-init: SNS topic + SQS queues auto-created on boot
- [x] Implement `IEventPublisher` + `SnsEventPublisher` in Api (Phase B ✅)
- [x] Implement `PollSqsJob`, `SyncPricesJob`, and event handlers in Worker (Phase C–D ✅)

### 22. CI/CD Pipeline ✅

> **Source:** `ProductController.cs:L93`

Setup CI/CD using GitHub Actions or Azure DevOps. Automate build, test, and deployment. Include unit tests and integration tests in the pipeline.

- [x] Create `.github/workflows/ci.yml`
- [x] Steps: Restore → Build → Test → (optional) Docker push
- [x] Add badge to README

### 23. API Documentation (Swagger) ✅

> **Source:** `ProductController.cs:L83`

Enhance Swagger/OpenAPI documentation with XML comments, request/response examples, status codes, and endpoint descriptions.

- [x] Enable XML documentation in `.csproj`
- [x] Add `/// <summary>` to all controller methods
- [x] Add `[ProducesResponseType]` attributes
- [x] Add example requests/responses

### 24. AutoMapper Integration ✅

> **Source:** `ProductController.cs:L75`

Implement AutoMapper to centralize mapping between domain models and DTOs. Reduce boilerplate in services.

- [x] Install `AutoMapper.Extensions.Microsoft.DependencyInjection`
- [x] Create `MappingProfile` with `Product ↔ ProductDto` maps
- [x] Replace manual `MapProductToProductDto` methods

---

## 🏗️ P2 — Architecture Evolution (Phase 2) ✅

> See `doc/PHASE2_PLAN.md` for full spec and diagrams.

### 16. Shared Contracts Enforcement

- [x] All entities centralized in `InventoryAlert.Domain` ✅
- [x] `global using` aliases in Api and Worker for backward compat ✅
- [x] Add `ArchitectureTests` project (NetArchTest.Rules) to enforce zero-duplication at CI
- [x] Move shared `AppSettings` config models to `Contracts.Configuration`

### 17. Standardized Event Types ✅

- [x] Reverse-DNS format: `inventoryalert.{domain}.{action}.v{version}`
- [x] `EventEnvelope` fields standardized: `EventType`, `Payload`, `CorrelationId`, `Source`
- [x] `EventTypes.IsKnown()` for O(1) dispatch validation
- [x] `IReadOnlySet<string>` for type registry

### 18. Event Handler Dispatcher Pattern ✅

- [x] `IEventHandler<TPayload>` interface defined in `Worker.Handlers`
- [x] Dictionary dispatcher in `PollSqsJob` — adding handler = 1 dictionary entry only
- [x] `UnknownEventHandler` fallback — logs warning + ACKs unknown types
- [x] All handlers implement `IEventHandler<T>` interface

### 19. Retry & Dead Letter Queue ✅

- [x] `ApproximateReceiveCount` guard — explicit DLQ push after > 3 receives
- [x] ACK-on-success-only pattern — failures leave message in SQS for natural redelivery
- [x] Configure SQS `RedrivePolicy` in `moto-init/init-sqs.sh` (MaxReceiveCount=3)
- [x] Create `inventory-dlq` queue in Moto init script
- [x] Add `SqsDlqUrl` to `appsettings.Docker.json` and `appsettings.json`

### 20. DynamoDB Integration ✅

- [x] **Phase 4A** — Register `IAmazonDynamoDB` client in `Worker/Program.cs`
- [x] **Phase 4A** — Create `EventLogDynamoRepository.cs` in `Worker/Persistence`
- [x] **Phase 4A** — Add `DynamoDbSettings` (table name) to `WorkerSettings`
- [x] **Phase 4A** — Create DynamoDB table in `moto-init/init-dynamodb.sh`
- [x] **Phase 4B** — Write event to DynamoDB after each successful dispatch (TTL=90d)
- [x] **Phase 4B** — Write `Status=failed` entry on handler exception
- [x] **Phase 4C** — Add `GET /api/events` endpoint to query DynamoDB event log

---

## 🛠️ Maintenance & CI/CD

- [x] **Fresh State enforcement** — Always run `dotnet clean` before major test/coverage runs to ensure `.editorconfig` changes and build artifacts are correctly synchronized.
- [x] **Coverage Script updated** — `code-coverage.bat` now executes `dotnet clean` before tests.
- [x] **CI Pipeline updated** — `.github/workflows/ci.yml` now executes `dotnet clean` in the Unit Test stage.

- **Command:** `dotnet clean && dotnet test`
- **Result:** Pure, non-stale test results.
- **Reference:** `.editorconfig` rules require clean/rebuild for strict enforcement in some environments.

> **Last Updated:** 2026-04-07
>
> **Legend:** 🔴 P0 = Do now | 🟠 P1 = Next sprint | 🟡 P2 = Data/Validation | 🏗️ P2 = Architecture | 🟢 P3 = Observability | 🟢 P4 = Performance | 🔵 P4 = Security | 🟣 P5 = Quality | ⚫ P6 = DevOps
