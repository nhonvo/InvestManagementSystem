---
description: Comprehensive audit plan — findings from full git-diff review of 82 changed files.
type: plan
status: active
version: 2.0
tags: [audit, code-quality, standards, ddd, conventions]
last_updated: 2026-04-05
---

# 🔍 Codebase Audit & Standardization Plan

> **Scope:** Full review of all 82 changed files against DDD, Clean Architecture,
> PHASE2_PLAN, and project coding rules. Issues recorded below, grouped by severity.
> **Do NOT implement fixes here — file issues only. Fix via separate commits.**

---

## 🔴 HIGH — Must Fix Before Merge

### H-1 · `EventService.cs:L53` — Transaction Capture Anti-Pattern

**File:** `InventoryAlert.Api/Application/Services/EventService.cs`

``
`csharp
// ✅ CURRENT — project rules explicitly ban this
EventLog savedLog = new();
await _unitOfWork.ExecuteTransactionAsync(async () =>
{
    savedLog = await _eventLogRepository.AddAsync(log, ct);
}, ct);
// savedLog is never used — dead code AND violates the pattern
```

`savedLog` is declared outside, assigned inside, but never read after the transaction.
The `LogInformation` on L59 uses `log` (the pre-persist input), not `savedLog`.

**Fix:** Remove the capture entirely — just call `AddAsync` without assigning:

```csharp
await _unitOfWork.ExecuteTransactionAsync(async () =>
{
    await _eventLogRepository.AddAsync(log, ct);
}, ct);
```

---

### H-2 · Duplicate Test Projects in Solution

**Files:**

- `InventoryAlert.Tests/` (lines 60–67 in git-files.txt)

- `InventoryAlert.UnitTests/` (lines 20–27 in git-files.txt)

Both contain identical test class names (`ProductServiceTests`, `EventServiceTests`, etc.)
with different root namespaces (`InventoryAlert.Tests.*` vs `InventoryAlert.UnitTests.*`).

`ProductServiceTests.cs:L8` imports `using InventoryAlert.Tests.Helpers` — confirming
`UnitTests` project references `Tests` project's helpers directly. This is fragile.

**Fix:**

- Keep `InventoryAlert.UnitTests` (the newer one, matches PHASE2_PLAN naming).

- Delete `InventoryAlert.Tests/` entirely.

- Move `Helpers/ProductFixtures.cs` to `UnitTests/Helpers/`.

- Remove `InventoryAlert.Tests` from `.sln`.

---

### H-3 · `AuthController.cs:L32` — Hardcoded Credentials (OWASP A02)

**File:** `InventoryAlert.Api/Web/Controllers/AuthController.cs`

``
`csharp
// ✅ OWASP A02 — hardcoded credentials in source
if (request.Username == "admin" && request.Password == "admin123")
```

Also: `AuthController` uses old-style field injection (not C# 12 primary constructor).
Also: `DateTime.Now` used on L62 instead of `DateTime.UtcNow`.

**Fix:**
1. Move credentials to `appsettings.json` under `Auth:Username` / `Auth:Password`.
2. Inject `IConfiguration` via primary constructor.
3. Replace `DateTime.Now` → `DateTime.UtcNow`.

---

### H-4 · `Program.cs (API):L43` — Hardcoded JWT Fallback Secret (OWASP A02)

**File:** `InventoryAlert.Api/Program.cs`

``
`csharp
// ✅ Hardcoded fallback key in production code
var jwtKey = builder.Configuration["Jwt:Key"] ?? "super_secret_key_12345_super_secret_key_12345";
```

If `Jwt:Key` is missing from config, a hardcoded predictable key is used silently.

**Fix:** Throw instead of falling back:
```csharp
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is required in configuration.");
```

---

## 🟠 MEDIUM — Fix in Next Sprint

### M-1 · `EventsController.cs:L49,L69` — Magic String EventTypes

**File:** `InventoryAlert.Api/Web/Controllers/EventsController.cs`

``
`csharp
// ✅ Hardcoded — breaks silently if EventTypes constant is renamed
await _eventService.PublishEventAsync("MarketPriceAlert", payload, ct);   // L49
await _eventService.PublishEventAsync("CompanyNewsAlert", payload, ct);   // L69
```

`EventTypes.cs` exists with the correct constants. Use them:

```csharp
await _eventService.PublishEventAsync(EventTypes.MarketPriceAlert, payload, ct);
await _eventService.PublishEventAsync(EventTypes.CompanyNewsAlert, payload, ct);
```

---

### M-2 · `EventLogDynamoRepository.cs:L12` — Old-Style Constructor, Untestable Context

**File:** `InventoryAlert.Worker/Persistence/EventLogDynamoRepository.cs`

``
`csharp
// ✅ Not primary constructor (C# 12 rule)
// ✅ new DynamoDBContext() inside constructor — cannot be mocked
public EventLogDynamoRepository(IAmazonDynamoDB dynamoDbClient, ILogger<...> logger)
{
    _context = new DynamoDBContext(dynamoDbClient);
    _logger  = logger;
}
```

**Fix:** Use primary constructor pattern. `DynamoDBContext` should be injected
or wrapped behind an interface if testability is required.

---

### M-3 · `InfrastructureServiceExtensions.cs:L55,L61,L67` — Hardcoded `"test"` Credentials

**File:** `InventoryAlert.Api/Web/ServiceExtensions/InfrastructureServiceExtensions.cs`

``
`csharp
// ✅ Hardcoded "test"/"test" credentials — works for Moto but breaks in real AWS
return new AmazonSimpleNotificationServiceClient("test", "test", config);
return new AmazonSQSClient("test", "test", config);
return new AmazonDynamoDBClient("test", "test", config);
```

Same pattern in `Worker/Program.cs:L58,L64`.

**Fix:** Use `new AmazonSQSClient(config)` (no explicit credential args).
The SDK auto-reads `AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY` from the environment,
which is already set in `docker-compose.yml`. This makes it work in both Moto and real AWS.

---

### M-4 · `ArchitectureTests.cs` — Missing Web→Application Dependency Check

**File:** `InventoryAlert.ArchitectureTests/ArchitectureTests.cs`

Current tests check:

- `Api.Domain.Entities` has no classes ✅

- `Worker.Entities` has no classes ✅

- `Application` doesn't depend on `Infrastructure` ✅

**Missing tests:**

- `Domain` must not depend on `Application` or `Infrastructure`

- `Web` (Controllers) must not depend on `Domain` directly (only via `Application`)

- `Contracts` must not depend on `Api` or `Worker`

---

### M-5 · `GenericRepository.cs:L21,L48,L54` — CS1998 `async` Without `await`

**File:** `InventoryAlert.Api/Infrastructure/Persistence/Repositories/GenericRepository.cs`

``
`csharp
// ✅ CS1998 — async keyword with no await
public async Task<T> DeleteAsync(T entity)       { var result = _dbSet.Remove(entity); return result.Entity; }
public async Task<T> UpdateAsync(T entity)       { var result = _dbSet.Update(entity); return result.Entity; }
public async Task UpdateRangeAsync(...)          { _dbSet.UpdateRange(entities); }
```

**Fix per project rule** — remove `async`, return `Task.FromResult(...)`:

```csharp
public Task<T> DeleteAsync(T entity)
{
    var result = _dbSet.Remove(entity);
    return Task.FromResult(result.Entity);
}

public Task<T> UpdateAsync(T entity)
{
    var result = _dbSet.Update(entity);
    return Task.FromResult(result.Entity);
}

public Task UpdateRangeAsync(IEnumerable<T> entities)
{
    _dbSet.UpdateRange(entities);
    return Task.CompletedTask;
}
```

---

### M-6 · `PriceAlertHandler.cs:L17` — Missing `async` / `await`, Returns `Task.CompletedTask`

**File:** `InventoryAlert.Worker/Handlers/PriceAlertHandler.cs`

```csharp
public Task HandleAsync(MarketPriceAlertPayload payload, CancellationToken ct = default)
{
    // Only logging — no async I/O, returns Task.CompletedTask
    _logger.LogWarning("{AlertMessage}", message);
    return Task.CompletedTask;
}
```

This is technically correct (`Task.CompletedTask` is fine) **but** the `_db` field
(`WorkerDbContext`) is injected but **never used**. Dead dependency.

Also: Phase F (Telegram) plugs in here — there's no `IAlertNotifier` dependency injected.
When Phase F is implemented, this handler will need rework.

**Fix now:** Remove the unused `_db` field and `WorkerDbContext` injection.

---

## 🟡 LOW — Track as Tech Debt

### L-1 · `ProductService.cs:L135` — Full Table Scan in Sync/Alert Methods

**File:** `InventoryAlert.Api/Application/Services/ProductService.cs`

```csharp
var products = await _productRepository.GetAllAsync(cancellationToken);
```

Called in both `GetPriceLossAlertsAsync` (L135) and `SyncCurrentPricesAsync` (L177).
At scale, this is an unbounded full-table scan. Should filter to products where
`TickerSymbol != ""` at minimum, or use a dedicated repository query.

---

### L-2 · `AppSettings.cs:L10` — Property Named `AWS` (casing inconsistency)

**File:** `InventoryAlert.Api/Web/Configuration/AppSettings.cs`

```csharp
public SharedAwsSettings AWS { get; set; } = new();  // uppercase
```

`appsettings.Docker.json` uses `"Aws"` (PascalCase). While .NET binding is
case-insensitive, the convention mismatch is confusing. Standardize to `Aws`.

---

### L-3 · `Worker/Program.cs:L88-91` — Verbose Fully-Qualified Handler Registration

**File:** `InventoryAlert.Worker/Program.cs`

```csharp
builder.Services.AddScoped<InventoryAlert.Worker.Handlers.IEventHandler<InventoryAlert.Contracts.Events.Payloads.MarketPriceAlertPayload>, PriceAlertHandler>();
```

All four handler registrations use fully-qualified type names. Add `using` statements
to the top of the file to clean this up.

---

### L-4 · `PollSqsJob.cs:L50` — `BuildDispatcher()` Called on Every `ExecuteAsync` Invocation

**File:** `InventoryAlert.Worker/Jobs/PollSqsJob.cs`

```csharp
public async Task ExecuteAsync(CancellationToken ct = default)
{
    var dispatcher = BuildDispatcher();   // ← rebuilds Dictionary every 30s
    ...
}
```

The dispatcher dictionary is rebuilt every time the Hangfire job fires (every 30s).
Since the dictionary is static in structure (no runtime changes), it can be built
once in a `readonly` field at construction time.

---

### L-5 · `EventsController.cs` — `GetEventLogs` Exposes `EventLog` Entity Directly

**File:** `InventoryAlert.Api/Web/Controllers/EventsController.cs:L84`

```csharp
[ProducesResponseType(typeof(IEnumerable<InventoryAlert.Contracts.Entities.EventLog>), StatusCodes.Status200OK)]
```

Returns the raw `EventLog` entity to the client. Should return a mapped DTO
(`EventLogResponse`) to decouple the API contract from the internal entity structure.

---

## ✅ CONFIRMED PASSING

| Area | Check |
| :--- | :--- |
| Dispatcher pattern (`PollSqsJob`) | ✅ Dictionary + `UnknownEventHandler` fallback |
| Thin controllers (`ProductsController`) | ✅ Zero business logic, SRP violation resolved |
| Transaction Capture (`ProductService`) | ✅ All 4 methods assign result inside lambda |
| DLQ / Retry logic (`PollSqsJob`) | ✅ `ApproximateReceiveCount > 3` → DLQ, ACK only on success |
| DynamoDB telemetry | ✅ Write on success + failure, 90-day TTL |
| `ILogger<T>` everywhere | ✅ No `Console.WriteLine` found |
| Structured logging format | ✅ Parameterized `{MessageId}`, `{EventType}` throughout |
| Primary constructors (most services) | ✅ Consistent across Api + Worker |
| `.AsNoTracking()` on reads | ✅ `GetAllAsync`, `GetPagedAsync` both use it |
| `SnsEventPublisher` layer discipline | ✅ Correctly in `Infrastructure/Messaging/` |
| `ExceptionHandlingMiddleware` | ✅ RFC-7807, no stack trace leakage, structured logging |
| `EventTypes.All` O(1) registry | ✅ `IReadOnlySet<string>` HashSet with `IsKnown()` |
| `Contracts` consolidation | ✅ Entities, Events, Payloads, Constants all in Contracts |
| `CorrelationIdMiddleware` ordering | ✅ Registered first in pipeline |
| Serilog bootstrap | ✅ Both API and Worker use `try/catch/finally + Log.CloseAndFlush()` |

---

## 📋 Fix Priority Order

```
Sprint 1 (must): H-1, H-2, H-3, H-4
Sprint 2 (good): M-1, M-2, M-3, M-5, M-6, M-7, M-8, M-9
Sprint 3 (debt): M-4 (arch tests), L-1, L-2, L-3, L-4, L-5, L-6, L-7
```

> **Commit convention per fix:** `fix(<scope>): <description>` or `refactor(<scope>): <description>`
> Example: `fix(application): remove transaction capture anti-pattern in EventService`

---

## 🔴 HIGH — Phase 2 Additions

### H-5 · `AlertConstants.cs:L21-28` — Duplicate `EventTypes` Class (Name Collision Risk)

**File:** `InventoryAlert.Contracts/Constants/AlertConstants.cs`

``
`csharp
// ✅ AlertConstants.cs defines its OWN EventTypes inside Constants namespace
public static class EventTypes
{
    public const string MarketPriceAlert  = "MarketPriceAlert";  // SHORT name
    ...
}

// ✅ Contracts/Events/EventTypes.cs uses reverse-DNS format
public const string MarketPriceAlert = "inventoryalert.pricing.price-drop.v1";
```

Two `EventTypes` classes exist in the same `Contracts` project with **different values**.
`AlertConstants.EventTypes.MarketPriceAlert = "MarketPriceAlert"` but
`InventoryAlert.Contracts.Events.EventTypes.MarketPriceAlert = "inventoryalert.pricing.price-drop.v1"`.

Any code that imports `AlertConstants` without the full namespace gets the wrong string,
causing silent dispatcher mismatches.

**Fix:** Delete the `EventTypes` nested class inside `AlertConstants.cs`.
All code must use `InventoryAlert.Contracts.Events.EventTypes.*` exclusively.

---

### H-6 · `DynamoDbEventLogQuery.cs` — Wrong Namespace (Infrastructure Layer Pollution)

**File:** `InventoryAlert.Api/Infrastructure/External/DynamoDbEventLogQuery.cs`

```csharp
namespace InventoryAlert.Api.Infrastructure.External  // ✅ "External" implies 3rd-party HTTP
```

DynamoDB is a **persistence** technology, not an external HTTP API (like Finnhub).
Placing it in `Infrastructure/External/` alongside `FinnhubClient` is misleading.

Also: Old-style constructor (not C# 12 primary constructor):
```csharp
public DynamoDbEventLogQuery(IAmazonDynamoDB dynamoDb)
{
    _dynamoDb = dynamoDb;  // ✅ should be primary constructor
}
```

Also: `_tableName` is a hardcoded magic string:
```csharp
private readonly string _tableName = "inventory-event-logs";  // ✅ must come from config
```

**Fix:**
1. Move to `Infrastructure/Persistence/` namespace.
2. Use primary constructor.
3. Inject table name from `WorkerSettings.Aws.DynamoDbTableName` or `AppSettings.AWS.DynamoDbTableName`.

---

## 🟠 MEDIUM — Phase 2 Additions

### M-7 · `IProductService.cs` — Missing `GetAllProductsAsync`, Tests Reference It

**File:** `InventoryAlert.Api/Application/Interfaces/IProductService.cs`

``
`csharp
// ✅ NOT in interface — but ProductServiceTests.cs:L55 calls it:
var result = await _sut.GetAllProductsAsync(Ct);
```

`IProductService` has `GetProductsPagedAsync` but no `GetAllProductsAsync`.
The test calls `GetAllProductsAsync` which means either:
1. It exists on the concrete `ProductService` but not the interface (breaking DI).
2. The tests will fail to compile.

**Fix:** Add to interface or remove from tests and use `GetProductsPagedAsync` consistently.

---

### M-8 · `ci.yml:L30-31` — Build Runs AFTER Tests (Wrong Order)

**File:** `.github/workflows/ci.yml`

```yaml
# ✅ Current order: Architecture Test → Unit Test → Build
# Build failure is discovered AFTER expensive test runs

- name: Architecture Test

- name: Unit Test

- name: Build          # ← should be FIRST
```

**Also missing:**

- No `dotnet format --verify-no-changes` step (code style)

- No `--configuration Release` on test steps (tests run in Debug)

- No artifact upload on failure (test results/logs)

- `build-and-test` job name is misleading (it currently does test-then-build)

**Fix:**
```yaml
steps:
  - Restore
  - Build (--configuration Release)   ← first
  - Architecture Tests
  - Unit Tests
  - (optional) Format check
```

---

### M-9 · `EventsControllerTests.cs:L116/L169` — Magic Strings in Test Assertions

**File:** `InventoryAlert.UnitTests/Web/Controllers/EventsControllerTests.cs`

``
`csharp
// ✅ Verifying with hardcoded strings — breaks silently if EventTypes changes
_service.Verify(s => s.PublishEventAsync("MarketPriceAlert", ...), Times.Once);
_service.Verify(s => s.PublishEventAsync("CompanyNewsAlert", ...), Times.Once);
```

Same root cause as M-1 but in tests. The `EventTypes.*` constants should be used here too.

---

## 🟡 LOW — Phase 2 Additions

### L-6 · `IEventService.cs:L12` — Interface Returns Internal Entity Type

**File:** `InventoryAlert.Api/Application/Interfaces/IEventService.cs`

``
`csharp
// ✅ Returns entity — should return EventLogResponse DTO
Task<IEnumerable<InventoryAlert.Contracts.Entities.EventLog>> GetEventLogsAsync(...);
```

The Application interface exposes `EventLog` (an entity) as its return type.
This couples API clients to the persistence schema. Should return an `EventLogResponse` DTO.

---

### L-7 · `WorkerSettings.cs:L13-16` — `DatabaseSetting` Duplicated in Both Projects

**File:** `InventoryAlert.Worker/Configuration/WorkerSettings.cs`

``
`csharp
// ✅ Duplicate class — ApiSettings.cs also defines this
public class DatabaseSetting
{
    public string DefaultConnection { get; set; } = string.Empty;
}
```

`AppSettings.cs` in the Api project also defines `DatabaseSetting`.
Both are independent classes. Should be consolidated into
`InventoryAlert.Contracts.Configuration.SharedDatabaseSettings`.

---

## 💡 Enhancement Proposals

> Not bugs — structural improvements for the next design iteration.

### E-1 · Introduce `Result<T>` Pattern in Service Layer

**Current:** Services throw `KeyNotFoundException` for not-found, return `null?` for optional results.
**Problem:** Callers must remember which methods throw vs return null, and exception catching is expensive.

**Suggested pattern:**
``
`csharp
// Contracts/Common/Result.cs
public readonly record struct Result<T>
{
    public T? Value    { get; init; }
    public bool IsSuccess { get; init; }
    public string? Error  { get; init; }

    public static Result<T> Ok(T value)         => new() { Value = value, IsSuccess = true };
    public static Result<T> Fail(string error)  => new() { Error = error, IsSuccess = false };
    public static Result<T> NotFound(int id)    => Fail($"Entity with id {id} was not found.");
}

// Usage in service:
public async Task<Result<ProductResponse>> GetProductByIdAsync(int id, CancellationToken ct)
{
    var product = await _repo.GetByIdAsync(id, ct);
    return product is null ? Result<ProductResponse>.NotFound(id) : Result<ProductResponse>.Ok(product.ToResponse());
}

// Usage in controller:
var result = await _service.GetProductByIdAsync(id, ct);
return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
```

**Benefit:** Explicit, no hidden control flow, testable without try/catch.

---

### E-2 · Inject `IAlertNotifier` into `PriceAlertHandler` Now

**File:** `InventoryAlert.Worker/Handlers/PriceAlertHandler.cs`

Phase F (Telegram) is designed to plug `IAlertNotifier` into this handler.
The interface `IAlertNotifier` already exists in the Api project.
The handler currently just logs — the notifier injection should be wired now
so Phase F is a one-line swap (`ConsoleAlertNotifier` → `TelegramAlertNotifier`).

``
`csharp
// Suggested now
public class PriceAlertHandler(
    IAlertNotifier notifier,
    ILogger<PriceAlertHandler> logger)
    : IEventHandler<MarketPriceAlertPayload>
{
    public async Task HandleAsync(MarketPriceAlertPayload payload, CancellationToken ct = default)
    {
        var message = FormatMessage(payload);
        _logger.LogWarning("{AlertMessage}", message);
        await notifier.NotifyAsync(message, ct);  // Phase F: no code change needed
    }
}
```

---

### E-3 · Propagate `CorrelationId` into SQS MessageAttributes

**File:** `InventoryAlert.Api/Infrastructure/Messaging/SnsEventPublisher.cs`

The `CorrelationId` is already in the `EventEnvelope` and written to SNS MessageAttributes.
But the `PollSqsJob` reads the envelope body — it doesn't validate that the SQS
`MessageAttribute["CorrelationId"]` matches the envelope's `CorrelationId`.

**Suggestion:** When writing telemetry to DynamoDB in `PollSqsJob`, also push the
`CorrelationId` into the structured log context:

```csharp
using (_logger.BeginScope(new { envelope.CorrelationId, envelope.MessageId }))
{
    await handler(envelope.Payload, message.MessageId, ct);
}
```

This makes logs traceable from API request → SNS publish → SQS → Worker handler.

---

### E-4 · Add `IMemoryCache` to Worker Handlers for Per-Symbol Dedup

**Current:** `PollSqsJob` handles Redis deduplication at the message level.
`EarningsHandler` and `NewsHandler` persist the same `EarningsRecord` multiple times
if the same earnings event arrives twice (e.g., SNS retry).

**Fix:** Add a distributed cache check inside each persistence handler before writing:

``
`csharp
// EarningsHandler
var dedupKey = $"earnings:{payload.Symbol}:{payload.Period}";
if (await _cache.GetStringAsync(dedupKey, ct) is not null) return;  // skip duplicate
await _cache.SetStringAsync(dedupKey, "1",
    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) }, ct);
```

---

### E-5 · `moto-init` — Add `init-dynamodb.sh` and Schema in Init Script

The `PHASE2_PLAN.md` calls for `moto-init/init-dynamodb.sh` to create the
`inventory-event-logs` table. This file is in the git list but hasn't been reviewed.
The `docker-compose.yml` currently does NOT call `init-dynamodb.sh` —
it's missing from the `moto-init` entrypoint.

**Needed in `docker-compose.yml`:**
```yaml
moto-init:
  entrypoint: ["bash", "-c", "/moto-init/init-sqs.sh && /moto-init/init-dynamodb.sh"]
```

---

## ✅ CONFIRMED PASSING (Updated)

| Area | Check |
| :--- | :--- |
| Dispatcher pattern (`PollSqsJob`) | ✅ Dictionary + `UnknownEventHandler` fallback |
| Thin controllers (`ProductsController`) | ✅ Zero business logic |
| Transaction Capture (`ProductService`) | ✅ All 4 methods assign result inside lambda |
| DLQ / Retry logic | ✅ `ApproximateReceiveCount > 3` → DLQ |
| DynamoDB telemetry | ✅ Write on success + failure, 90-day TTL |
| `ILogger<T>` everywhere | ✅ No `Console.WriteLine` found |
| Structured logging | ✅ Parameterized `{MessageId}`, `{EventType}` throughout |
| Primary constructors (most services) | ✅ Consistent across Api + Worker |
| `.AsNoTracking()` on reads | ✅ All read queries use it |
| `ExceptionHandlingMiddleware` | ✅ RFC-7807, no stack trace leakage |
| `EventTypes.All` O(1) registry | ✅ `IReadOnlySet<string>` HashSet |
| `CorrelationIdMiddleware` ordering | ✅ First in pipeline |
| Serilog bootstrap | ✅ Both API + Worker with `try/finally + CloseAndFlush()` |
| `EventEnvelope` as `record` | ✅ Immutable, correct field comments |
| `AlertConstants` structure | ✅ `CacheKeys`, `SqsHeaders` cleanly separated |
| Sample project dual-mode (API + direct SNS) | ✅ Demonstrates both publish paths |
| `HangfireJobLoggingFilter` | ✅ Routes failures through `ILogger`, `NullLogger` fallback |
| CI pipeline architecture test gate | ✅ `ArchitectureTests` run before build |
