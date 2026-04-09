# Worker Engine

## Full Worker Architecture

```mermaid
graph TD
    HF[Hangfire Scheduler] -->|Trigger every 5m| FPSW[FinnhubPricesSyncWorker]
    HF -->|Trigger every 1m| MSW[MarketStatusWorker]
    FPSW -->|GET /quote| Finnhub[Finnhub API]
    FPSW -->|Persist| DB[(PostgreSQL)]
    FPSW -->|Publish PriceSyncedEvent| SQS[(Amazon SQS)]
    SQS -->|Consume| AH[AlertRuleHandler]
    MSW -->|Cache isOpen| Cache[In-Memory Cache]
    Cache --> AH
    AH -->|Evaluate conditions| RULES[AlertRule Engine]
    RULES -->|If triggered| NH[NotificationHandler]
    NH -->|sendMessage| Telegram[Telegram Bot API]
    NH --> DB
```

## Scheduled Workers

| Worker | Schedule | Purpose |
|---|---|---|
| `FinnhubPricesSyncWorker` | Every 5 minutes | Fetches latest prices, updates DB, publishes `PriceSyncedEvent` |
| `MarketStatusWorker` | Every 1 minute | Checks if market is open; blocks alert evaluation when closed |
| `NewsCheckWorker` | Hourly | Scrapes company news from Finnhub to DynamoDB |

### FinnhubPricesSyncWorker — Step by Step

1. Fetch all active `Product` records from PostgreSQL using **paged processing** (batch size: 50)
2. For each symbol: call `GET /quote` on Finnhub
3. If `currentPrice` is valid:
   - Update `Product.CurrentPrice`
   - Insert a `PriceHistory` row
   - Publish `PriceSyncedEvent` to SQS
4. **Context Maintenance**: Call `_dbContext.ChangeTracker.Clear()` after each paged batch to ensure execution memory stays flat.
5. If `currentPrice` is invalid: log a warning and skip — **never throw**

### NewsCheckWorker — Step by Step

1. Fetch all tracking symbols from DB.
2. Call `GET /news` on Finnhub for the last 24-hour window.
3. Deduplicate news entries using `FinnhubId` against existing DynamoDB records.
4. If news count > 0:
    - Perform **BatchSave** to DynamoDB (chunked by 25).
    - Log completion metrics.
5. Throttling: Uses `SemaphoreSlim(2)` to limit concurrent HTTP calls to Finnhub.

### Internal Queue (SQS)

- **Polling**: Long-polling with a 20-second wait time
- **Visibility Timeout**: 30 seconds
- **DLQ**: Failed messages moved after 3 retries
- **Message Types**: `PriceSyncedEvent`, `AlertTriggeredEvent`

---

## Hangfire Dashboard

Navigate to `http://localhost:8080/hangfire` (requires Admin role).

| Dashboard Tab | Purpose |
|---|---|
| Enqueued | Jobs waiting to be picked up |
| Processing | Jobs currently executing |
| Succeeded | Completed jobs with execution time |
| Failed | Jobs that threw exceptions — click to retry |
| Recurring | Scheduled jobs and their cron expressions |

> **Business Impact**: If `FinnhubPricesSyncWorker` fails, prices are stale and alert evaluation is paused until the next successful sync cycle.
