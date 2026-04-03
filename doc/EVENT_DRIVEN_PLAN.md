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

## 🖥️ Docker Services (Final State)

```yaml
services:
  api:          # REST API + Event Publisher
  worker:       # Hangfire + SQS Consumer (new)
  db:           # PostgreSQL
  redis:        # Cache + Hangfire storage
  moto:         # Mock SNS/SQS
```

### Worker Dockerfile
The `InventoryAlert.Worker` project will have its own build stage in `docker-compose.yml`, sharing the same base image and Postgres/Redis connection strings via environment variables.

---

## ✅ Implementation Checklist

### Phase A — Contracts & Infrastructure
- [ ] Create `InventoryAlert.Contracts` project
- [ ] Define `EventEnvelope` record
- [ ] Define all Payload records (Price, Earnings, Insider, News)
- [ ] Add Contracts reference to Api and Worker projects

### Phase B — API (Publisher Side)
- [ ] Create `IEventPublisher` interface
- [ ] Implement `SnsEventPublisher` (using AWSSDK.SQS/SNS pointing to Moto)
- [ ] Create `EventLog` entity + EF Migration
- [ ] Create `EventsController` with POST endpoints
- [ ] Register `IEventPublisher` in DI

### Phase C — Worker Project
- [ ] Create `InventoryAlert.Worker` as a Worker Service
- [ ] Install Hangfire + `Hangfire.PostgreSql`
- [ ] Create `PollSqsJob` — SQS dequeue + event routing
- [ ] Create `SyncPricesJob` — replaces current `FinnhubSyncWorker`
- [ ] Create `EarningsCheckJob` — calls `/stock/earnings`
- [ ] Create `InsiderTxCheckJob` — calls `/stock/insider-transactions`
- [ ] Create `NewsCheckJob` — calls `/company-news`
- [ ] Implement Redis caching inside each job
- [ ] Add Worker to `docker-compose.yml`

### Phase D — Event Handlers (inside Worker)
- [ ] `PriceAlertHandler` → log + trigger Telegram notification
- [ ] `EarningsHandler` → persist `EarningsRecord` to DB
- [ ] `InsiderHandler` → persist `InsiderTransaction` to DB
- [ ] `NewsHandler` → persist `NewsRecord` to DB

### Phase E — Sample Publisher
- [ ] Create `InventoryAlert.Sample` console app
- [ ] Implement HTTP client posting to `/api/events`
- [ ] Implement direct-to-Moto-SQS publisher (bypass API)
- [ ] Add interactive menu with all event types

### Phase F — Telegram Integration
- [ ] Create `TelegramAlertNotifier` implementing `IAlertNotifier`
- [ ] Add `Telegram:BotToken` and `Telegram:ChatId` to app settings
- [ ] Register notifier in DI (replace `ConsoleAlertNotifier` in Worker)
- [ ] Add `TELEGRAM_BOT_TOKEN` to `.env.example`

---

> **Priority Order:** A → B → C → D → E → F
>
> The existing `FinnhubSyncWorker` in the API should be **removed** once `SyncPricesJob` in the Worker is live. The Worker owns all background logic.
