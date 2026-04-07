---
description: Plan for Contracts consolidation, DynamoDB telemetry, event handler pattern, and retry/DLQ strategy
type: plan
status: active
version: 1.0
tags: [architecture, contracts, dynamodb, events, retry, dlq]
last_updated: 2026-04-05
---

# 🏗️ Phase 2 Architecture Plan

> **Scope:** Shared-project consolidation, standardized event handling, retry/DLQ mechanism, and DynamoDB integration for event telemetry.

---

## 1. 📦 Shared Project Consolidation (`InventoryAlert.Contracts`)

### Problem

`InventoryAlert.Api` and `InventoryAlert.Worker` each had their own copy of domain entities, causing subtle divergence bugs. The fix was started in Phase 1 but needs to be enforced with a hard architectural gate.

### Target Structure

```text
InventoryAlert.Contracts/
├── Entities/
│   ├── Product.cs            ✅ Live
│   ├── EventLog.cs           ✅ Live
│   ├── EarningsRecord.cs     ✅ Live
│   ├── InsiderTransaction.cs ✅ Live
│   └── NewsRecord.cs         ✅ Live
├── Events/
│   ├── EventEnvelope.cs      ✅ Updated — EventType + Payload + CorrelationId
│   ├── EventTypes.cs         ✅ Updated — reverse-DNS naming
│   └── Payloads/             ✅ All payload types
└── Constants/
    └── AlertConstants.cs     ✅ Live — CacheKeys, EventTypes, SqsHeaders
```

### Rules (enforced by DDD skill)

- `InventoryAlert.Api` imports: `Contracts` only (via `global using` aliases in `SharedEntityAliases.cs`).
- `InventoryAlert.Worker` imports: `Contracts` only (via `SharedEntityAliases.cs`).
- No entity definition may exist in `Api.Domain.Entities` or `Worker.*` — any addition goes to `Contracts` first.

### Remaining Tasks

- [x] Add `ArchitectureTests` project using `NetArchTest.Rules` to enforce zero-duplication at CI time.
- [x] Move `AppSettings` Finnhub/AWS sections to `Contracts.Configuration` so both services share config models.

---

## 2. 🎯 Event Handler Pattern (Dispatcher Architecture)

### Problem

`PollSqsJob` used a `switch` statement that required modifying the job class itself whenever a new event type was added — violating OCP.

### Solution Implemented ✅

```text
SQS Message → PollSqsJob
                  │
                  ▼
         Dictionary Dispatcher
          (EventType → Func)
                  │
        ┌─────────┴─────────────────────────────────┐
        │         │             │                   │
   PriceAlert  Earnings    InsiderSell          CompanyNews
  Handler<T>  Handler<T>  Handler<T>           Handler<T>
                                                     │
                                         (unknown EventType)
                                                     │
                                          UnknownEventHandler
                                      (logs warning + ACKs)
```

### Interface Contract

```csharp
public interface IEventHandler<in TPayload>
{
    Task HandleAsync(TPayload payload, CancellationToken ct = default);
}
```

### Adding a New Handler

1. Add a constant to `EventTypes.cs`:
   ```csharp
   public const string MyNewEvent = "inventoryalert.domain.action.v1";
   ```
2. Add payload to `Contracts/Events/Payloads/MyNewEventPayload.cs`.
3. Create `Worker/Handlers/MyNewHandler.cs` implementing `IEventHandler<MyNewEventPayload>`.
4. Register in `Worker/Program.cs`: `builder.Services.AddScoped<IEventHandler<MyNewEventPayload>, MyNewHandler>()`.
5. Add one entry to the `BuildDispatcher()` dictionary in `PollSqsJob`.

---

## 3. 🔁 Retry & Dead Letter Queue (DLQ) Mechanism

### Design

```
SQS Queue (MaxReceiveCount=3)
    │
    ├── Attempt 1: Handler throws → no ACK → message invisible for VisibilityTimeout
    ├── Attempt 2: SQS redelivers → fails again
    ├── Attempt 3: SQS redelivers → fails again
    │
    └── SQS RedrivePolicy → DLQ automatically (after 3 receives)
            │
            └── Optional: PollSqsJob.MoveToDlqAsync() for explicit manual DLQ push
```

### Configuration (SQS Redrive Policy — in Moto init script)

```bash
aws sqs set-queue-attributes \
  --queue-url http://localhost:4566/000000000000/inventory-queue \
  --attributes '{
    "RedrivePolicy": "{
      \"deadLetterTargetArn\": \"arn:aws:sqs:us-east-1:000000000000:inventory-dlq\",
      \"maxReceiveCount\": \"3\"
    }"
  }'
```

### Responsibility Split

| Scenario | Action | Responsibility |
| :--- | :--- | :--- |
| Handler throws | Do NOT delete — SQS redelivers | `PollSqsJob` (by design) |
| Receive count > 3 | MoveToDlq + delete from main queue | `PollSqsJob.MoveToDlqAsync()` |
| Unknown EventType | Log warning + ACK (DELETE) | `UnknownEventHandler` |
| Bad JSON | Log + MoveToDlq + ACK | `PollSqsJob.ProcessMessageAsync()` |
| Duplicate (Redis deduplicated) | ACK without processing | `PollSqsJob` |

### Tasks

- [ ] Update `moto-init/init-sqs.sh` to create `inventory-dlq` queue and configure RedrivePolicy.
- [ ] Add `SqsDlqUrl` to `appsettings.Docker.json` and `appsettings.json`.
- [ ] Add a DLQ monitor/alerter (future: Lambda or Worker polling DLQ for notifications).

---

## 4. 🗄️ DynamoDB Integration Plan

### Why DynamoDB?

| Requirement | PostgreSQL | DynamoDB |
| :--- | :--- | :--- |
| Relational CRUD (Products) | ✅ Perfect fit | Overkill |
| High-throughput event logs | ❌ Table grows unbounded | ✅ Designed for it |
| TTL-based auto-expiry | ❌ Requires cron jobs | ✅ Native TTL per-item |
| Deduplication store | ❌ Requires explicit cleanup | ✅ TTL + conditional writes |
| SQS message audit trail | ❌ Schema migrations | ✅ Schema-less, append-only |

### Target: EventLog Table

```text
Table: inventory-event-logs

PK:  EventType   (String) — partition key
SK:  MessageId   (String) — sort key (unique per event)

Attributes:
  Source        — emitting service
  Payload       — raw JSON (for audit trail)
  CorrelationId — cross-service trace ID
  ProcessedAt   — ISO8601 UTC string
  TTL           — Unix epoch (auto-delete after 90 days)
  Status        — "processed" | "failed" | "skipped"
```

### Required NuGet

```powershell
dotnet add InventoryAlert.Worker package AWSSDK.DynamoDBv2
```

> Already installed ✅

### Implementation Phases

#### Phase 4A — Infrastructure Setup

- [ ] Add `DynamoDbSettings` to `WorkerSettings` (table name, endpoint).
- [ ] Register `IAmazonDynamoDB` in `Worker/Program.cs`.
- [ ] Create `Worker/Persistence/EventLogDynamoRepository.cs`.
- [ ] Create DynamoDB table in `moto-init/init-dynamodb.sh`.

#### Phase 4B — Telemetry Write

- [ ] In `PollSqsJob`, after successful dispatch: write event to DynamoDB with `Status="processed"`.
- [ ] On handler failure: write with `Status="failed"` before re-throwing.
- [ ] Set TTL = `ProcessedAt + 90 days`.

#### Phase 4C — Query API (optional)

- [ ] Add `GET /api/events?symbol=AAPL&limit=20` endpoint in Api.
- [ ] Api calls DynamoDB via `IAmazonDynamoDB` (query by EventType + date range).
- [ ] Dashboard "Alerts Feed" view connects to this endpoint.

### DynamoDB Table Schema

```csharp
public class EventLogEntry
{
    [DynamoDBHashKey("EventType")]
    public string EventType { get; set; } = string.Empty;

    [DynamoDBRangeKey("MessageId")]
    public string MessageId { get; set; } = string.Empty;

    public string Source        { get; set; } = string.Empty;
    public string Payload       { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string ProcessedAt   { get; set; } = string.Empty;
    public string Status        { get; set; } = string.Empty;

    [DynamoDBProperty("TTL")]
    public long Ttl { get; set; }   // Unix epoch for DynamoDB native TTL
}
```

---

## 📋 Task Checklist

### Contracts ✅

- [x] Centralize all entities in `InventoryAlert.Contracts`
- [x] Add `Constants/AlertConstants.cs`
- [x] Standardize `EventEnvelope` fields
- [ ] Add architecture enforcement tests

### Event Handler Pattern ✅

- [x] `IEventHandler<TPayload>` interface
- [x] Dictionary dispatcher in `PollSqsJob`
- [x] `UnknownEventHandler` fallback
- [x] All existing handlers implement `IEventHandler<T>`

### Event Types ✅

- [x] Reverse-DNS naming convention (`inventoryalert.domain.action.v1`)
- [x] `EventTypes.IsKnown()` for dispatch validation
- [x] `IReadOnlySet<string>` for O(1) lookup

### Retry / DLQ ✅

- [x] `ApproximateReceiveCount` check (> 3 → manual DLQ)
- [x] ACK-only-on-success pattern in dispatcher
- [x] Moto `init-sqs.sh` RedrivePolicy configuration
- [x] `SqsDlqUrl` in appsettings files

### DynamoDB ✅

- [x] Phase 4A: Infrastructure setup
- [x] Phase 4B: Telemetry write on process/fail
- [x] Phase 4C: Query API endpoint

---

> **Next:** Run `/feature-flow` to implement DynamoDB Phase 4A.
