# 🏗️ Event-Driven Architecture Plan

## Hangfire + SNS/SQS + Redis + Multi-Project System

> **Scope:** Extend the existing `InventoryAlert.Api` into a full event-driven microservices ecosystem.
> **Constraint:** Use only Finnhub Free-Tier APIs. All AWS services emulated via **Moto**.

---

## 🗺️ High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         EXTERNAL WORLD                              │
│   [Telegram Bot]   [Sample.Publisher App]   [Browser/Postman]       │
└───────────┬─────────────────┬───────────────────────┬──────────────┘
            │                 │                       │
            ▼                 ▼                       ▼
┌────────────────────────────────────────────────────────────────────┐
│                    InventoryAlert.Api  (Port 8080)                  │
│                                                                     │
│  POST /api/events          →  Validate → Publish to SNS Topic       │
│  POST /api/market-alerts   →  Manually trigger a price-alert event  │
│  GET  /api/products        →  (existing endpoints)                  │
└───────────────────────────────────┬────────────────────────────────┘
                                    │ SNS Publish
                                    ▼
┌──────────────────────────────────────────────────────────────────┐
│                    Moto (Mock AWS) Port 5000                      │
│                                                                   │
│  SNS Topic:  arn:aws:sns:us-east-1:123456789:inventory-events    │
│     │                                                             │
│     └──► SQS Queue: arn:aws:sqs:us-east-1:123456789:event-queue │
└──────────────────────────────────────────────────────────────────┘
                         │ SQS Poll
                         ▼
┌──────────────────────────────────────────────────────────────────┐
│             InventoryAlert.Worker  (New Project)                  │
│                                                                   │
│  [Hangfire Job: PollSqsJob]                                       │
│     → Dequeue messages from SQS                                   │
│     → Route by event type                                         │
│     → Execute handlers (FetchQuote, SaveAlert, etc.)              │
│     → Cache results in Redis                                      │
│     → Persist to PostgreSQL                                       │
└──────────────────────────────────────────────────────────────────┘
                         │ Cache Reads
                         ▼
┌──────────────────────────────────────────────────────────────────┐
│                    Redis  (Port 6379)                             │
│                                                                   │
│  Key: "product:quote:{symbol}"   → TTL 60s  (live price cache)   │
│  Key: "job:last-run:{jobName}"   → TTL 10m  (last execution ts)  │
│  Key: "alert:history:{symbol}"  → TTL 24h  (dedup alert history) │
└──────────────────────────────────────────────────────────────────┘
```

---

## 📁 Solution Structure

```
InventoryManagementSystem/
├── InventoryAlert.Api/          ← Existing REST API
├── InventoryAlert.Worker/       ← NEW: Hangfire + SQS Consumer
├── InventoryAlert.Sample/       ← NEW: Sample Event Publisher CLI
├── InventoryAlert.Contracts/    ← NEW: Shared Event Schemas (DTOs)
└── InventoryAlert.Tests/        ← Existing test project
```

---

## 📦 Project 1: `InventoryAlert.Contracts` (Shared Event Schema)

> **Why a separate project?** Both the API (publisher) and the Worker (consumer) must agree on the same message format. A shared `Contracts` project (referenced by both) eliminates duplication and version drift.

### Event Envelope (SNS/SQS Standard Format)

All events follow the standard SNS notification format:

```json
{
  "Type": "Notification",
  "MessageId": "uuid-v4",
  "TopicArn": "arn:aws:sns:us-east-1:123456789:inventory-events",
  "Subject": "MarketPriceAlert",
  "Message": "{...serialized payload...}",
  "Timestamp": "2026-04-04T01:00:00Z",
  "MessageAttributes": {
    "EventType": {
      "Type": "String",
      "Value": "MarketPriceAlert"
    },
    "SourceService": {
      "Type": "String",
      "Value": "InventoryAlert.Api"
    }
  }
}
```

### Event Types

| EventType | Trigger | Finnhub Source |
| :--- | :--- | :--- |
| `MarketPriceAlert` | Price drops beyond `PriceAlertThreshold` | `/quote` |
| `StockLowAlert` | `StockCount` drops below `StockAlertThreshold` | Internal DB |
| `EarningsAlert` | A tracked company announces earnings | `/stock/earnings` |
| `InsiderSellAlert` | Insider selling detected above a threshold | `/stock/insider-transactions` |
| `CompanyNewsAlert` | High-impact news published for a tracked stock | `/company-news` |

### New Domain Models

```csharp
// Contracts/Events/EventEnvelope.cs
public record EventEnvelope
{
    public string MessageId { get; init; } = Guid.NewGuid().ToString();
    public string TopicArn { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;  // EventType
    public string Message { get; init; } = string.Empty;  // Serialized payload
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Dictionary<string, string> MessageAttributes { get; init; } = [];
}

// Contracts/Events/Payloads/MarketPriceAlertPayload.cs
public record MarketPriceAlertPayload
{
    public int ProductId { get; init; }
    public string Symbol { get; init; } = string.Empty;
    public decimal OriginPrice { get; init; }
    public decimal CurrentPrice { get; init; }
    public decimal DropPercent { get; init; }  // e.g. 0.12 = 12%
}

// Contracts/Events/Payloads/EarningsAlertPayload.cs
public record EarningsAlertPayload
{
    public string Symbol { get; init; } = string.Empty;
    public decimal ActualEPS { get; init; }
    public decimal EstimatedEPS { get; init; }
    public decimal SurprisePercent { get; init; }
    public string Period { get; init; } = string.Empty;  // e.g. "Q4 2025"
}

// Contracts/Events/Payloads/InsiderSellAlertPayload.cs
public record InsiderSellAlertPayload
{
    public string Symbol { get; init; } = string.Empty;
    public string InsiderName { get; init; } = string.Empty;
    public long SharesSold { get; init; }
    public decimal ValueUsd { get; init; }
    public DateTime TransactionDate { get; init; }
}

// Contracts/Events/Payloads/CompanyNewsAlertPayload.cs
public record CompanyNewsAlertPayload
{
    public string Symbol { get; init; } = string.Empty;
    public string Headline { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public DateTime PublishedAt { get; init; }
}
```

---

## 🌐 Project 2: `InventoryAlert.Api` (New Endpoints)

### New Endpoints

| Method | Route | Purpose |
| :--- | :--- | :--- |
| `POST` | `/api/events` | Receive any generic event and publish to SNS |
| `POST` | `/api/events/market-alert` | Manually trigger a `MarketPriceAlert` event |
| `POST` | `/api/events/news-alert` | Manually trigger a `CompanyNewsAlert` event |
| `GET`  | `/api/events/types` | List all supported event types |

### New Service: `IEventPublisher`

```
IEventPublisher
  └── PublishAsync(EventEnvelope envelope) → Task
       → Serializes envelope to JSON
       → Publishes to SNS Topic ARN
       → SNS auto-fans-out to subscribed SQS queues
```

### New Entity: `EventLog` (for audit trail)

Persist all published events to the DB for auditing:

| Column | Type | Notes |
| :--- | :--- | :--- |
| `Id` | `int` | PK |
| `MessageId` | `string` | From SNS |
| `EventType` | `string` | e.g. `MarketPriceAlert` |
| `Payload` | `string` | JSON body |
| `PublishedAt` | `DateTime` | UTC |
| `SourceService` | `string` | e.g. `InventoryAlert.Api` |

---

## ⚙️ Project 3: `InventoryAlert.Worker` (New Project)

This is a separate **.NET Worker Service** (minimal API or `BackgroundService` host) focused entirely on job processing.

### Why separate from the API?

- The API should stay fast and focused on handling HTTP.
- The Worker can be scaled independently (more workers = more SQS throughput).
- Hangfire runs in the Worker, keeping the Dashboard away from the API layer.

### Architecture: Hangfire Jobs

| Job Name | Schedule | Responsibility | Finnhub Call |
| :--- | :--- | :--- | :--- |
| `PollSqsJob` | Every 30s | Dequeue messages from SQS and dispatch to handlers | None |
| `SyncPricesJob` | Every 10m | Update `CurrentPrice` for all products | `/quote` |
| `EarningsCheckJob` | Every 6h | Check if tracked symbols have new earnings | `/stock/earnings` |
| `InsiderTxCheckJob` | Every 6h | Check for insider selling on tracked symbols | `/stock/insider-transactions` |
| `NewsCheckJob` | Every 1h | Fetch latest news for tracked symbols | `/company-news` |

### SQS Consumer Flow

```
PollSqsJob runs →
  Dequeue up to 10 messages from SQS →
  For each message:
    1. Parse EventEnvelope
    2. Check Redis: "alert:history:{symbol}" to avoid duplicate alerts
    3. Route to handler by EventType:
       - MarketPriceAlert  → PriceAlertHandler → Notify (log/Telegram)
       - EarningsAlert     → EarningsHandler  → Update Product metrics in DB
       - InsiderSellAlert  → InsiderHandler   → Flag product in DB
       - CompanyNewsAlert  → NewsHandler      → Store news in DB
    4. Cache result in Redis (TTL rules above)
    5. Delete message from SQS (acknowledge)
```

### Redis Caching Strategy

| Key Pattern | Stores | TTL | Populated By |
| :--- | :--- | :--- | :--- |
| `product:quote:{AAPL}` | Latest price quote | 60 seconds | `SyncPricesJob` |
| `alert:history:{AAPL}` | Last alert timestamp | 24 hours | `PollSqsJob` (dedup) |
| `job:last-run:EarningsCheckJob` | Last run timestamp | 10 minutes | Each Job |
| `news:{AAPL}:latest` | Latest news headline | 1 hour | `NewsCheckJob` |

---

## 🧪 Project 4: `InventoryAlert.Sample` (Event Publisher CLI)

A simple Console App that **simulates external systems** pushing events into the ecosystem. This is for local development and testing without needing a real frontend.

### Functionality

```
InventoryAlert.Sample (Console App)
  ├── Menu option 1: Publish a MarketPriceAlert event
  ├── Menu option 2: Publish an EarningsAlert event
  ├── Menu option 3: Publish an InsiderSellAlert event
  ├── Menu option 4: Publish a CompanyNewsAlert event
  └── Menu option 5: Stress test — publish 50 random events
```

### How it works

- Points to `http://localhost:8080/api/events` (the main API).
- Sends events via **HTTP POST** with the standard `EventEnvelope` format.
- OR can publish directly to **Moto SQS** at `http://localhost:5000` bypassing the API (for testing the worker in isolation).

---

## 🔌 Telegram Integration (Future)

The `IAlertNotifier` interface (already planned) will have a `TelegramAlertNotifier` implementation:

```
TelegramAlertNotifier
  → Uses Telegram Bot API (bot token in env vars)
  → Sends formatted messages to a chat channel
  → Triggered by PriceAlertHandler / InsiderHandler inside the Worker
```

Message format:

```
🚨 PRICE DROP ALERT
Symbol:    AAPL
Drop:      -15.3%
Current:   $142.00
Origin:    $167.50
Threshold: -10%
Time:      2026-04-04 02:00 UTC
```

---

## 🧩 Business Logic Scenarios (Finnhub Free Tier)

| Scenario | Entities Affected | Finnhub Endpoint | Event Published |
| :--- | :--- | :--- | :--- |
| **Price Drop > threshold** | `Product.CurrentPrice` | `/quote` | `MarketPriceAlert` |
| **Earnings miss > 10%** | New `EarningsRecord` entity | `/stock/earnings` | `EarningsAlert` |
| **Insider sells > $1M** | New `InsiderTransaction` entity | `/stock/insider-transactions` | `InsiderSellAlert` |
| **High impact news** | New `NewsRecord` entity | `/company-news` | `CompanyNewsAlert` |
| **Market closes** | Trigger close-of-day sync | `/stock/market-status` | Internal only |

### New Entities (Database Changes Required)

```
EarningsRecord
  ├── Id (int)
  ├── ProductId (FK → Product)
  ├── Symbol (string)
  ├── Period (string)       ← e.g. "2025Q4"
  ├── ActualEPS (decimal)
  ├── EstimatedEPS (decimal)
  ├── SurprisePercent (decimal)
  └── RecordedAt (DateTime)

InsiderTransaction
  ├── Id (int)
  ├── ProductId (FK → Product)
  ├── InsiderName (string)
  ├── TransactionType (string)  ← "Buy" or "Sell"
  ├── Shares (long)
  ├── ValueUsd (decimal)
  └── TransactionDate (DateTime)

NewsRecord
  ├── Id (int)
  ├── ProductId (FK → Product)
  ├── Symbol (string)
  ├── Headline (string)
  ├── Source (string)
  ├── Url (string)
  └── PublishedAt (DateTime)

EventLog
  ├── Id (int)
  ├── MessageId (string)   ← From SNS
  ├── EventType (string)
  ├── Payload (string)     ← JSON
  ├── Status (string)      ← "Published" | "Processed" | "Failed"
  └── CreatedAt (DateTime)
```

---

## 🚀 Future Enterprise Extension (DynamoDB Polyglot Strategy)

When raw telemetry events outgrow Postgres and Redis connections bottleneck:

1. **Replaces Postgres `EventLog`:** A DynamoDB `InventoryAlert_EventLog` table would use `ExpiresAt` (Epoch) for AWS-native auto-cleanup, shedding millions of stale events without requiring massive SQL `DELETE` sweeps.
2. **Replaces Redis Deduplication:** DynamoDB has persistent TTL semantics. Instead of limiting memory in Redis clusters, worker nodes can do a cost-efficient atomic `PutItem` to check if a `MessageId` or `alert:history:{symbol}` exists, naturally expiring stale histories.
3. **Merges Timeseries Metrics:** Rather than maintaining `EarningsRecord`, `InsiderTransaction`, and `NewsRecord` across multiple SQL tables with foreign keys, a single NoSQL table `InventoryAlert_StockHistory` stores it all via composite Sort Keys (`NEWS#2026-04-04`, `EARNINGS#2026Q4`), creating a blazing-fast audit trail view.

---

## 🖥️ Docker Services (Final State)

```yaml
services:
  api:       # REST API + Event Publisher
  worker:    # Hangfire + SQS Consumer
  db:        # PostgreSQL 17-alpine    — healthcheck: pg_isready
  redis:     # Redis 7.2-alpine        — healthcheck: redis-cli ping
  moto:      # motoserver/moto:5.1.22  — Mock SNS/SQS, healthcheck: curl :5000
  moto-init: # amazon/aws-cli:2.3.4    — one-shot init: creates SNS + SQS on boot
```

> **Startup order enforced by `depends_on`:**
> `db` + `redis` + `moto` (healthchecks) → `moto-init` (completed) → `api` + `worker`

### Worker Dockerfile

Located at `InventoryAlert.Worker/Dockerfile`. Uses `dotnet/runtime:10.0`
(not `aspnet`) since the Worker has no HTTP listener.

---

## ✅ Implementation Checklist

### Phase A — Contracts & Infrastructure

- [x] Create `InventoryAlert.Contracts` project
- [x] Define `EventEnvelope` record
- [x] Define all Payload records (Price, Earnings, Insider, News, StockLow)
- [x] Add Contracts reference to Api, Worker, and Sample projects

### Phase B — API (Publisher Side)

- [x] Create `IEventPublisher` interface
- [x] Implement `SnsEventPublisher` (using AWSSDK.SQS/SNS pointing to Moto)
- [x] Create `EventLog` entity + EF Migration
- [x] Create `EventsController` with POST endpoints
- [x] Register `IEventPublisher` in DI

### Phase C — Worker Project

- [x] Create `InventoryAlert.Worker` as a Worker Service
- [x] Install Hangfire + `Hangfire.PostgreSql`
- [x] Add Worker to `docker-compose.yml` (with Dockerfile + appsettings.Docker.json)
- [x] Moto init script: `SolutionFolder/moto-init/init-sqs.sh` creates SNS + SQS
- [x] Create `PollSqsJob` — SQS dequeue + event routing
- [x] Create `SyncPricesJob` — replaces current `FinnhubSyncWorker`
- [x] Create `EarningsCheckJob` — calls `/stock/earnings`
- [x] Create `InsiderTxCheckJob` — calls `/stock/insider-transactions`
- [x] Create `NewsCheckJob` — calls `/company-news`
- [x] Implement Redis caching inside each job
- [x] Add Worker to `docker-compose.yml`

### Phase D — Event Handlers (inside Worker)

- [x] `PriceAlertHandler` → log + trigger Telegram notification
- [x] `EarningsHandler` → persist `EarningsRecord` to DB
- [x] `InsiderHandler` → persist `InsiderTransaction` to DB
- [x] `NewsHandler` → persist `NewsRecord` to DB

### Phase E — Sample Publisher

- [x] Create `InventoryAlert.Sample` console app
- [x] Implement HTTP client posting to `/api/events`
- [x] Implement direct-to-Moto-SQS publisher (bypass API)
- [x] Add interactive menu with all event types

### Phase F — Telegram Integration

- [x] Create `TelegramAlertNotifier` implementing `IAlertNotifier`
- [x] Add `Telegram:BotToken` and `Telegram:ChatId` to AppSettings
- [x] Register notifier in DI (replaced `ConsoleAlertNotifier` in API)
- [x] Add `TELEGRAM_BOT_TOKEN` to `appsettings.Example.json`

---

### Phase G — Shared Entity Migration ✅

> Moved all domain entities shared between API and Worker out of `InventoryAlert.Api.Domain.Entities`
> into `InventoryAlert.Contracts.Entities` — single source of truth.

- [x] `Product` → `InventoryAlert.Contracts.Entities.Product`
- [x] `EventLog` → `InventoryAlert.Contracts.Entities.EventLog`
- [x] `EarningsRecord` → `InventoryAlert.Contracts.Entities.EarningsRecord`
- [x] `InsiderTransaction` → `InventoryAlert.Contracts.Entities.InsiderTransaction`
- [x] `NewsRecord` → `InventoryAlert.Contracts.Entities.NewsRecord`
- [x] All `using` statements updated in Api, Worker, and Tests projects
- [x] `AWSSDK.SimpleNotificationService` + `AWSSDK.SQS` installed in `InventoryAlert.Sample`
- [x] Full solution build: **0 errors** ✅
- [x] Full test run: **61/61 passed** ✅

**EF Configuration strategy:** Each service keeps its own `DbContext` with inline EF fluent configs.
`Contracts` remains a plain POCO library with no EF dependency — clean separation.

---

> **Priority Order:** A → B → C → D → E → F → G
>
> The existing `FinnhubSyncWorker` in the API has been **retired** — `SyncPricesJob` in the Worker now owns all background price sync logic.

