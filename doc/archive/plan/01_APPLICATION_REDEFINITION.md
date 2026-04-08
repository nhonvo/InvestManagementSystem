# 01 — Application Redefinition
> **SystemName:** Market Pulse (working title)
> **Previous scope:** InventoryAlert — price breach alerts only
> **New scope:** Full stock/crypto intelligence platform with chat triggers, centralized logging, and event-driven architecture

---

- plan build auth by idenity
- centralize external finhub if can and build strawman and flag need
- build a pattern jobs in worker like wrap all logic like access db, call finnhub as jobs return (result, jobsstatus) 
- JobResult = success, failed, skipped
- success and skipped deleted message and failed retry 3 times before go to DLQ
- Remove DynamoDB `EventLog` table and migrate all telemetry to Centralized Structured Logging (ELK/Kibana)
- refactor worker review api and worker project log handle exception, ...mapping pattern
- review and enhance seed data
- update doc readme and build doc for new developer and how to run or maintain project, draw some chart about system architecture, data flow, sequence diagram, ...


## 1. What This Application Is

Market Pulse is an **event-driven stock intelligence platform** that:
- Aggregates financial data from Finnhub (quotes, news, profiles, earnings, recommendations)
- Delivers insights through a **Web UI**, a **Telegram bot**, and **push notifications**
- Reacts to market events asynchronously through a job/event pipeline
- Supports **stocks and crypto** (user-selected symbols from a crawled Finnhub list)
- Centralizes logs through an OpenTelemetry → ELK pipeline

This is **NOT** just an alert system. Alerts are one output channel among several.

---

## 2. Event Taxonomy (Redefined)

Stop using raw domain state names as events. Events describe **something that happened**, not a data dump.

### 2.1 Domain Events (internal, SNS → SQS)

| Event Name                     | Trigger                                            | Payload (minimal)                                   |
|--------------------------------|----------------------------------------------------|-----------------------------------------------------|
| `stock.price.updated`          | SyncPricesJob detects a new quote                  | `{ symbol, price, change, changePercent, ts }`      |
| `stock.price.alert.crossed`    | Price crosses user threshold                       | `{ symbol, price, threshold, direction, userId }`   |
| `stock.news.published`         | NewsCheckJob finds new article                     | `{ symbol, headline, url, source, publishedAt }`    |
| `market.news.ingested`         | MarketNewsJob pulls general market news            | `{ category, headline, url, source, publishedAt }`  |
| `stock.recommendation.updated` | Weekly job detects consensus change                | `{ symbol, buy, hold, sell, strongBuy, period }`    |
| `stock.earnings.reported`      | EarningsJob detects new quarter result             | `{ symbol, actual, estimate, surprise, period }`    |
| `symbol.added`                 | User adds a symbol via UI/API                      | `{ symbol, type: stock or crypto, userId }`         |
| `symbol.removed`               | User removes a symbol                              | `{ symbol, userId }`                                |
| `alert.rule.created`           | User defines an alert rule                         | `{ ruleId, symbol, field, operator, threshold }`    |
| `alert.rule.triggered`         | A rule condition evaluates to true                 | `{ ruleId, symbol, value, threshold }`              |

### 2.2 Notification Events (outbound, one-way)

| Event                         | Channel         | Handler              |
|-------------------------------|-----------------|----------------------|
| `stock.price.alert.crossed`   | Telegram + Push | `PriceAlertHandler`  |
| `stock.news.published`        | Telegram        | `NewsHandler`        |
| `stock.earnings.reported`     | Telegram        | `EarningsHandler`    |
| `market.news.ingested`        | Telegram (opt)  | `MarketNewsHandler`  |

### 2.3 Telegram Bot Command Events (synchronous triggers)

| Command                       | Action                                     |
|-------------------------------|--------------------------------------------|
| `/price AAPL`                 | Returns real-time quote immediately        |
| `/news AAPL`                  | Returns 5 latest news for symbol           |
| `/recommend AAPL`             | Returns analyst recommendations            |
| `/earnings AAPL`              | Returns last 4 quarters EPS               |
| `/profile AAPL`               | Returns company profile                    |
| `/watchlist`                  | Lists user's subscribed symbols            |
| `/alert AAPL price > 200`     | Creates a price threshold alert rule       |
| `/market`                     | Returns current market status              |
| `/status`                     | Returns system health                      |

---

## 3. Data Ownership Matrix

| Data Type                  | Store      | Table / Key Pattern               | TTL  | Rationale                               |
|----------------------------|------------|-----------------------------------|------|----------------------------------|
| Products (symbols)         | PostgreSQL | `Products`                        | None | CRUD, relational, user-owned             |
| Alert Rules                | PostgreSQL | `AlertRules`                      | None | Relational, joins with Products          |
| User Watchlist             | PostgreSQL | `Watchlists`                      | None | Relational                               |
| Company Profiles (cache)   | PostgreSQL | `CompanyProfiles`                 | None | Refresh weekly, stable reference         |
| Company News               | DynamoDB   | `inventory-news`                  | 30d  | Time-series, high write, no joins        |
| Market News                | DynamoDB   | `inventory-market-news`           | 7d   | High volume, read once, ephemeral        |
| Price Snapshots (history)  | DynamoDB   | `inventory-price-history`         | 90d  | Append-only, time-keyed                  |
| Analyst Recommendations    | DynamoDB   | `inventory-recommendations`       | 90d  | One per symbol per period                |
| Earnings (quarterly)       | DynamoDB   | `inventory-earnings`              | 2y   | Quarterly, rarely updated                |
| Event Audit Log            | ELK Stack  | `mp-{service}-{YYYY.MM.DD}`       | 90d  | Replaces DynamoDB `inventory-event-logs`|
| Telegram Chat State        | Redis      | `telegram:session:{chatId}`       | 1h   | Ephemeral conversation context           |
| Price Alert Cooldown       | Redis      | `alert:history:{symbol}`          | 24h  | Dedup alerting within window             |
| Message Dedup Cache        | Redis      | `inventoryalert:processed:{msgId}` | 48h | SQS idempotency guard                   |
| Real-time Quote Cache      | Redis      | `quote:{symbol}`                  | 1m   | Rate-limit buffer for Finnhub API        |

---

## 4. Component Responsibility

### API (InventoryAlert.Api)
- CRUD: Products, AlertRules, Watchlists
- Publish domain events to SNS on mutations
- Read-only queries for Profiles, News, Recommendations, Earnings from DynamoDB/Postgres
- Health check and actuator endpoints
- OpenTelemetry traces → ELK

### Worker (InventoryAlert.Worker)
- All scheduled Hangfire jobs
- SQS consumer (`PollSqsJob`) — dispatches to handlers
- Telegram bot service (`TelegramBotService`)
- All Finnhub API calls live here (rate-limit controlled)
- OpenTelemetry traces → ELK

### UI (InventoryAlert.UI — new Next.js)
- Watchlist dashboard with live price tiles
- Per-symbol page: profile, news, recommendations, earnings charts
- Alert rule manager
- Market news feed
- Admin: system health, log drill-down (via Kibana link)

---

## 5. Log Strategy

### 5.1 Log Levels (enforced across all services)

| Level       | When to use                                               |
|-------------|-----------------------------------------------------------|
| Trace       | Internals — step-by-step paths (dev only, never prod)     |
| Debug       | Diagnostic values, args, return values (dev only)         |
| Information | "Something that matters happened" — job start/end, event received, entity created |
| Warning     | Recoverable issue — Finnhub null, retry #1, cache miss    |
| Error       | Operation failed — handler threw, DynamoDB write failed   |
| Critical    | System-level failure — startup crash, DLQ overflow        |

### 5.2 Required Structured Fields (every log line)

```json
{
  "service": "api | worker",
  "traceId": "{otel-trace-id}",
  "spanId": "{otel-span-id}",
  "symbol": "{optional}",
  "eventType": "{optional}",
  "userId": "{optional}",
  "jobId": "{optional}",
  "level": "Information"
}
```

### 5.3 Pipeline

```
App (Serilog)
  → OpenTelemetry Serilog Sink
    → OTEL Collector (otelcol)
      → Elasticsearch → Kibana (recommended)
```

**Dev stack (docker-compose additions):**
- `elasticsearch:8` on port 9200
- `kibana:8` on port 5601
- `otelcol` with OTLP receiver configured

**Index naming:** `mp-{service}-{YYYY.MM.DD}`

**Required Kibana dashboards:**
- Error rate by service and level
- Hangfire job execution timeline
- Finnhub API call rate vs rate-limit threshold
- SQS DLQ depth over time
- Event throughput by EventType

---

## 6. Detailed Task Breakdown

### 6.1 Authentication & Security (Identity)
- **Engine**: ASP.NET Core Identity with Entity Framework Core (Postgres).
- **Format**: JWT Bearer tokens for API/UI communication.
- **Roles**: `User` (Subscribed symbols, alert rules) and `Admin` (Job management, system health).
- **Features**: Registration, Login, and Password Reset (simulated via logs).

### 6.2 Data Seeding & Reliability
- **Tool**: Use `Bogus` library to generate realistic financial entities.
- **Coverage**: Initial 50 NASDAQ/NYSE symbols, company profiles, and 3-month sample price history.
- **Environment**: Seed logic runs automatically in `Development` and `Docker` environments if the database is empty.

### 6.3 Project Health: Logging & Exceptions
- **Global Exceptions**: Implement `IProblemDetailsService` (ASP.NET Core 8+) or a custom Middleware to return RFC 7807 `ProblemDetails` for all unhandled errors.
- **Log Enrichment**: 
    - Use Serilog `LogContext.PushProperty` to attach `JobId` or `Symbol` to every log line in the worker handlers.
    - Implement a `DiagnosticEventListener` for EF Core to log slow queries (>500ms) as Warnings.
- **Mapping**: Standardize on `Direct Mapping` for entities -> events to avoid AutoMapper complexity in high-throughput worker loops.
