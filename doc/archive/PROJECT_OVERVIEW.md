---
description: Deep technical reference for the InventoryManagementSystem solution — architecture, layers, event pipeline, storage, and CI/CD.
type: reference
status: active
version: 3.0
tags: [architecture, ddd, event-driven, infrastructure, reference]
last_updated: 2026-04-05
---

# 🏗️ Inventory Alert System — Technical Architecture

> This document covers **architecture, infrastructure, and system internals only**.
> For business workflows and REST API listing, see [`FEATURES_OVERVIEW.md`](FEATURES_OVERVIEW.md).

---

## 1. Solution Project Map

| Project | SDK | Role |
|---|---|---|
| `InventoryAlert.Api` | `Microsoft.NET.Sdk.Web` | REST API — Controllers, Application Services, Infrastructure |
| `InventoryAlert.Worker` | `Microsoft.NET.Sdk.Web` | Background processor — Hangfire jobs, SQS consumers, Event Handlers |
| `InventoryAlert.Contracts` | `Microsoft.NET.Sdk` | Shared kernel — Entities, Events, Constants. Zero dependencies. |
| `InventoryAlert.Sample` | `Microsoft.NET.Sdk` | Local test publisher — bypasses API to directly push events to SNS |
| `InventoryAlert.UnitTests` | `Microsoft.NET.Sdk` | xUnit + Moq + FluentAssertions unit test suite |
| `InventoryAlert.IntegrationTests` | `Microsoft.NET.Sdk` | Testcontainers.PostgreSql integration tests |
| `InventoryAlert.ArchitectureTests` | `Microsoft.NET.Sdk` | NetArchTest.Rules — automated DDD boundary enforcement |

---

## 2. Clean Architecture / DDD Layer Rules

```
InventoryAlert.Contracts       ← innermost (no deps)
       ↑
InventoryAlert.Api
  ├─ Domain/Interfaces/        ← Repository + UoW contracts
  ├─ Application/Services/     ← Business logic (ProductService, EventService)
  ├─ Application/Interfaces/   ← IProductService, IEventService, IFinnhubClient
  ├─ Application/DTOs/         ← Request/Response + FinnhubQuoteResponse
  ├─ Application/Mappings/     ← Manual map extensions (no AutoMapper)
  ├─ Application/Validators/   ← FluentValidation rule classes
  ├─ Infrastructure/Persistence/  ← AppDbContext, GenericRepository, UnitOfWork
  ├─ Infrastructure/External/     ← FinnhubClient (HTTP), SnsEventPublisher, DynamoEventLogRepository
  └─ Web/Controllers/          ← Thin controllers, delegate to Application
```

**Enforced automatically by `InventoryAlert.ArchitectureTests`** (`NetArchTest.Rules`):
- `Application.*` **must not** depend on `Infrastructure.*`
- `Web.Controllers` **must not** directly depend on `Infrastructure.*`
- All classes in `Application.Services` **must** end with `"Service"`
- All classes in `Web.Controllers` **must** inherit `ControllerBase` and end with `"Controller"`
- All classes in `Worker.Handlers` **must** end with `"Handler"`
- `InventoryAlert.Contracts` **must not** import `Microsoft.EntityFrameworkCore` or `Microsoft.AspNetCore`
- All `*Repository` interfaces **must** expose only `Task`/`Task<T>` return types (no sync I/O)

---

## 3. `InventoryAlert.Contracts` — Shared Kernel

The `Contracts` project has **zero external NuGet dependencies** and is the single source of truth for:

### Domain Entities

| Class | Key Fields |
|---|---|
| `Product` | `Id`, `Name`, `TickerSymbol`, `OriginPrice`, `CurrentPrice`, `PriceAlertThreshold` (double, e.g. `0.2` = 20%), `StockCount`, `StockAlertThreshold`, `LastAlertSentAt` (nullable DateTime for alert cooldown) |
| `EventLog` | DynamoDB audit record per published event |
| `EarningsRecord` | Persisted EPS data from Finnhub earnings events |
| `NewsRecord` | Persisted company news headline |
| `InsiderTransaction` | Persisted insider sell transaction |

### Event Pipeline Types

**`EventEnvelope` record** — the standard wrapper for every SNS/SQS message:
```csharp
public record EventEnvelope
{
    public string MessageId    { get; init; }  // Guid — deduplication key
    public string EventType    { get; init; }  // Must match EventTypes constant
    public string Source       { get; init; }  // e.g. "InventoryAlert.Api"
    public string Payload      { get; init; }  // JSON-serialized inner payload
    public DateTime Timestamp  { get; init; }
    public string CorrelationId { get; init; } // Cross-service trace ID
}
```

**`EventTypes` static class** — canonical reverse-DNS event names:

| Constant | Value |
|---|---|
| `MarketPriceAlert` | `inventoryalert.pricing.price-drop.v1` |
| `StockLowAlert` | `inventoryalert.inventory.stock-low.v1` |
| `EarningsAlert` | `inventoryalert.fundamentals.earnings.v1` |
| `InsiderSellAlert` | `inventoryalert.fundamentals.insider-sell.v1` |
| `CompanyNewsAlert` | `inventoryalert.news.headline.v1` |

---

## 4. Event-Driven Pipeline (Publish → Consume)

### Publishing (inside `InventoryAlert.Api`)

1. A service or controller calls `IEventService.PublishEventAsync<TPayload>(eventType, payload, ct)`.
2. `EventService` constructs an `EventEnvelope`, stamps it with a `CorrelationId`, serializes it to JSON, and persists an `EventLog` record to **DynamoDB**.
3. `SnsEventPublisher` pushes the envelope JSON to the **AWS SNS Topic** (`inventory-events`).

### Consuming (inside `InventoryAlert.Worker`)

1. Hangfire runs `PollSqsJob` on a schedule; it long-polls `inventory-event-queue` (SQS bound to SNS).
2. Each message body is deserialized into `EventEnvelope`.
3. An internal **Dictionary Dispatcher** reads `EventType` and invokes the corresponding `IEventHandler<TPayload>`:

| EventType | Handler | Action |
|---|---|---|
| `MarketPriceAlert` | `PriceAlertHandler` | Logs a structured `🚨 PRICE DROP ALERT` warning; Telegram plug-in point |
| `EarningsAlert` | `EarningsHandler` | Persists `EarningsRecord` to PostgreSQL via `WorkerDbContext` |
| `CompanyNewsAlert` | `NewsHandler` | Persists `NewsRecord` to PostgreSQL |
| `InsiderSellAlert` | `InsiderHandler` | Persists `InsiderTransaction` to PostgreSQL |
| *(unknown)* | `UnknownEventHandler` | Logs and discards gracefully — no crash |

4. **Dead-Letter Queue**: After `ApproximateReceiveCount` exceeds 3, SQS automaticaly routes to `inventory-event-dlq`.

---

## 5. Storage Layer

| Store | Purpose | Access Pattern |
|---|---|---|
| **PostgreSQL** (Npgsql EF Core 10) | `Product`, `EarningsRecord`, `NewsRecord`, `InsiderTransaction` | `IProductRepository` → `GenericRepository<T>` → `AppDbContext` |
| **AWS DynamoDB** | `EventLog` audit trail (append-only, TTL-expiring) | `DynamoEventLogRepository`, accessed via `IEventService` |
| **IMemoryCache** (ASP.NET) | `Product` single-item cache (`Product_{id}`, 10 min TTL) | `ProductService.GetProductByIdAsync` — cache-aside pattern |
| **Hangfire** | Job scheduling metadata | Stored in PostgreSQL (`WorkerDbContext`) |

### Transaction Pattern (`ExecuteTransactionAsync`)
All writes use the project-mandated capture pattern:
```csharp
// ✅ Result captured inside lambda — safe even if lambda throws
ProductResponse result = null!;
await _unitOfWork.ExecuteTransactionAsync(async () =>
{
    var updated = await _productRepository.UpdateAsync(existing);
    result = updated.ToResponse();
}, cancellationToken);
return result;
```

---

## 6. Authentication

`AuthController` issues **JWT Bearer tokens** (`HmacSha256`, 2-hour expiry).  
Credentials are pulled from `appsettings.json → Auth:Username / Auth:Password`.  
All `ProductsController` endpoints are decorated `[Authorize]`.  
`EventsController` is unauthenticated (internal/service-to-service calls).

---

## 7. Docker / Local AWS Infrastructure

```yaml
# docker-compose.yml services
postgres      ← PostgreSQL 16, port 5432
moto          ← Moto (AWS mock), port 5000
moto-init     ← Runs init-sqs.sh + init-dynamodb.sh on startup
api           ← InventoryAlert.Api (depends on postgres + moto)
worker        ← InventoryAlert.Worker (depends on api + moto)
```

Moto init scripts (`SolutionFolder/moto-init/`):
- `init-sqs.sh` — creates `inventory-event-queue` + `inventory-event-dlq`
- `init-dynamodb.sh` — creates `EventLogs` table; reads `$AWS_ENDPOINT_URL` so it works from inside the Docker network

All Docker files are linked as **Solution Items** in `InventoryManagementSystem.sln` for direct editing in Visual Studio.

---

## 8. Testing Strategy

| Suite | Count | Tools |
|---|---|---|
| `InventoryAlert.UnitTests` | 66 tests | xUnit, Moq, FluentAssertions, EF InMemory |
| `InventoryAlert.IntegrationTests` | 5 tests | Testcontainers.PostgreSql |
| `InventoryAlert.ArchitectureTests` | 9 tests | NetArchTest.Rules |
| **Total** | **80 tests, 0 failures** | |

Key unit test rules:
- `ExecuteTransactionAsync` mocks **must** invoke the delegate: `.Returns<Func<Task>, CancellationToken>((action, _) => action())`
- `IDisposable.Dispose()` on test fixtures **must** call `GC.SuppressFinalize(this)` (CA1816)
- No `Thread.Sleep`, no `async` method without `await`

---

## 9. Observability

- **Serilog** structured logging → `logs/inventoryalert.log` (rolling file sink)
- **`CorrelationIdMiddleware`** — stamps every HTTP request with a `X-Correlation-Id` header; propagated through SNS `CorrelationId` field into DynamoDB `EventLog`
- **`ExceptionHandlingMiddleware`** — catches all unhandled exceptions and returns RFC-7807 `ProblemDetails` JSON (no internal stack traces exposed to clients)
