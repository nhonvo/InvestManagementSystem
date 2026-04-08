# 04 — Jobs, Workers & Telegram Bot Plan

---

## 1. Scheduled Jobs (Hangfire — InventoryAlert.Worker)

| Job Class              | Schedule        | Finnhub Endpoint         | Output Event / Action                    |
|------------------------|-----------------|--------------------------|------------------------------------------|
| `SyncPricesJob`        | Every 1 min     | `/quote`                 | Writes to Redis + DynamoDB price-history; publishes `stock.price.updated`; evaluates AlertRules → `stock.price.alert.crossed` |
| `NewsCheckJob`         | Every 5 min     | `/company-news`          | Deduplicates by FinnhubId, publishes `stock.news.published` for new articles |
| `MarketNewsJob`        | Every 15 min    | `/news?category=general` | Stores to `inventory-market-news`, publishes `market.news.ingested` |
| `RecommendationsJob`   | Daily 06:00 UTC | `/stock/recommendation`  | Upserts `inventory-recommendations`, publishes `stock.recommendation.updated` if changed |
| `EarningsJob`          | Daily 07:00 UTC | `/stock/earnings`        | Upserts `inventory-earnings`, publishes `stock.earnings.reported` if new quarter |
| `ProfileSyncJob`       | Weekly Sunday   | `/stock/profile2`        | Refreshes PostgreSQL `CompanyProfiles`   |
| `SymbolCrawlJob`       | Daily 02:00 UTC | `/stock/symbol`          | Populates reference list in Redis for UI dropdown |
| `MarketStatusJob`      | Every 5 min     | `/stock/market-status`   | Caches in Redis `market:status:{exchange}`, TTL 5 min |
| `EarningsCalendarJob`  | Daily 08:00 UTC | `/calendar/earnings`     | Syncs upcoming earnings to Redis (7-day window) |

### 1.1 Standardized Job Pattern (The Result Flow)

All jobs (Hangfire & SQS Handlers) must follow a unified result-oriented execution pattern to ensure consistent logging and failure handling.

```csharp
public enum JobStatus { Success, Failed, Skipped, PartiallySucceeded }

public record JobResult(
    JobStatus Status, 
    string Message = "", 
    int ProcessedCount = 0, 
    Exception? Error = null);

// Unified Pattern Example
public async Task<JobResult> ExecuteAsync(...) {
    try {
        // 1. Validation Logic (Return Skipped if no work)
        // 2. Strawman Gatekeeper call (Finnhub)
        // 3. Database operation (Postgres/DynamoDB)
        // 4. Return Success
    } catch (Exception ex) {
        // 5. Log structured error + Return Failed
    }
}
```

### 1.2 Finnhub Centralization (The Strawman Pattern)

To avoid scattered API keys and 429 Errors, all Finnhub calls are routed through a `FinnhubClient` with global rate-limiting.
- **Circuit Breaker**: Trips if Finnhub returns >5 consecutive 5xx errors.
- **Rate-limit Guard**: Uses a SempahoreSlim(60) to block concurrent calls beyond the free-tier limit.
- **Fail-Safe**: If call fails, return `JobResult.Failed` but do NOT crash the worker loop.

### Rate Limit Budget (Finnhub free: 60 req/min)

| Job                  | Symbols (est.) | Calls/run | Calls/hour    |
|----------------------|----------------|-----------|---------------|
| SyncPricesJob        | 20             | 20        | 1,200         |
| NewsCheckJob         | 20             | 20        | 240           |
| MarketNewsJob        | 4 categories   | 4         | 16            |
| RecommendationsJob   | 20             | 20        | 0.83 (daily)  |
| EarningsJob          | 20             | 20        | 0.83 (daily)  |
| **Total peak/min**   |                |           | ~25 req/min ✅ |

> Always add jitter between symbol iterations: `await Task.Delay(Random.Shared.Next(100, 400), ct)`

---

## 2. Event Handlers (SQS Consumer via `PollSqsJob`)

| EventType                      | Handler                      | Action                                                          |
|--------------------------------|------------------------------|-----------------------------------------------------------------|
| `stock.price.updated`          | `PriceUpdateHandler`         | Write to DynamoDB `inventory-price-history`                     |
| `stock.price.alert.crossed`    | `PriceAlertHandler`          | Notify via Telegram + push notification                         |
| `stock.news.published`         | `NewsHandler`                | Write to DynamoDB `inventory-news`, notify subscribed users     |
| `market.news.ingested`         | `MarketNewsHandler`          | Write to DynamoDB `inventory-market-news`                       |
| `stock.recommendation.updated` | `RecommendationHandler`      | Write to DynamoDB `inventory-recommendations`                   |
| `stock.earnings.reported`      | `EarningsHandler`            | Write to DynamoDB `inventory-earnings`, notify via Telegram     |
| `symbol.added`                 | `SymbolAddedHandler`         | Trigger immediate profile + quote sync for new symbol           |
| `alert.rule.created`           | `AlertRuleHandler`           | Load rule into Redis for fast evaluation by SyncPricesJob       |

### 2.1 SQS Reliability & DLQ Strategy

Retry logic is managed by the `SqsDispatcher` (Path B) or Hangfire (Path A).
- **Retry Count**: Exactly 3 attempts (tracked via `ApproximateReceiveCount` SQS attribute).
- **Result Logic**:
    - **Success**: Delete message immediately from SQS.
    - **Skipped**: Log rationale (e.g. "Deduplicated"), then Delete message.
    - **Failed**: 
        - If `< 3 retries`: Let SQS Re-drive (Visibility Timeout).
        - If `>= 3 retries`: Move to `inventory-event-dlq` and log "Poison Message".
- **DLQ Alarm**: Alert sent via Telegram if DLQ count > 0.

### Architecture
```
Telegram Webhook → POST /telegram/webhook (internal route in Worker)
                     → TelegramBotService.DispatchAsync(update)
                       → CommandRouter → ICommandHandler<T>
                         → [reads Redis cache | calls API via HttpClient | reads DynamoDB]
                           → sends reply via Telegram Bot API
```

### Command Handlers

| Command                         | Handler Class              | Data Source                                       |
|---------------------------------|----------------------------|---------------------------------------------------|
| `/price {SYMBOL}`               | `PriceCommandHandler`      | Redis `quote:{symbol}` → Finnhub `/quote`         |
| `/news {SYMBOL} [limit]`        | `NewsCommandHandler`       | DynamoDB `inventory-news`                         |
| `/recommend {SYMBOL}`           | `RecommendCommandHandler`  | DynamoDB `inventory-recommendations`              |
| `/earnings {SYMBOL}`            | `EarningsCommandHandler`   | DynamoDB `inventory-earnings`                     |
| `/profile {SYMBOL}`             | `ProfileCommandHandler`    | PostgreSQL `CompanyProfiles`                      |
| `/watchlist`                    | `WatchlistCommandHandler`  | API `GET /api/v1/watchlist`                       |
| `/add {SYMBOL}`                 | `AddSymbolCommandHandler`  | API `POST /api/v1/watchlist/{symbol}`             |
| `/alert {SYMBOL} price > {N}`   | `AlertCommandHandler`      | API `POST /api/v1/alerts`                         |
| `/market`                       | `MarketStatusCommandHandler` | Redis `market:status:US`                        |
| `/status`                       | `StatusCommandHandler`     | API `GET /health`                                 |
| `/help`                         | `HelpCommandHandler`       | Static text                                       |

### Telegram Bot Setup
- Register webhook: `POST https://api.telegram.org/bot{TOKEN}/setWebhook`
- Worker exposes: `POST /telegram/webhook` (internal, not on public `api` port)
- Session state stored in Redis: `telegram:session:{chatId}` TTL 1h
- Rate limit Telegram replies: max 1 message/second per chat

### Proactive Notifications (from Event Handlers)
```csharp
// Called by PriceAlertHandler, EarningsHandler, NewsHandler
await _telegramClient.SendMessageAsync(chatId,
    $"🚨 *{symbol}* crossed ${threshold} — now ${price}",
    ParseMode.Markdown);
```

---

## 4. Worker DI Registration Additions

```csharp
// Telegram
builder.Services.AddSingleton<ITelegramBotClient>(sp =>
    new TelegramBotClient(settings.Telegram.BotToken));
builder.Services.AddScoped<TelegramBotService>();

// Command handlers
builder.Services.AddScoped<PriceCommandHandler>();
builder.Services.AddScoped<NewsCommandHandler>();
// ... etc

// New jobs
builder.Services.AddScoped<MarketNewsJob>();
builder.Services.AddScoped<RecommendationsJob>();
builder.Services.AddScoped<EarningsJob>();
builder.Services.AddScoped<ProfileSyncJob>();
builder.Services.AddScoped<SymbolCrawlJob>();

// New event handlers
builder.Services.AddScoped<IEventHandler<PriceUpdatePayload>, PriceUpdateHandler>();
builder.Services.AddScoped<IEventHandler<MarketNewsPayload>, MarketNewsHandler>();
builder.Services.AddScoped<IEventHandler<EarningsPayload>, EarningsHandler>();
```

---

## 5. New DynamoDB Repositories to Create

Following the `DynamoDbGenericRepository<T>` pattern:

| Repository Class             | Entity Class             | Table                         |
|------------------------------|--------------------------|-------------------------------|
| `PriceHistoryDynamoRepository` | `PriceHistoryEntry`    | `inventory-price-history`     |
| `MarketNewsDynamoRepository`   | `MarketNewsDynamoEntry`| `inventory-market-news`       |
| `RecommendationRepository`     | `RecommendationEntry`  | `inventory-recommendations`   |
| `EarningsDynamoRepository`     | `EarningsEntry`        | `inventory-earnings`          |

All entities follow the same pattern as `NewsDynamoEntry`:
- `[DynamoDBTable("table-name")]` on class
- `[DynamoDBHashKey]` + `[DynamoDBRangeKey]` on PK/SK
- `Ttl` property for expiry

---

## 6. Hangfire Job Schedule Configuration

```csharp
// In JobSchedulerService.cs
RecurringJob.AddOrUpdate<SyncPricesJob>("sync-prices",       x => x.ExecuteAsync(CancellationToken.None), "*/1 * * * *");
RecurringJob.AddOrUpdate<NewsCheckJob>("news-check",         x => x.ExecuteAsync(CancellationToken.None), "*/5 * * * *");
RecurringJob.AddOrUpdate<MarketNewsJob>("market-news",       x => x.ExecuteAsync(CancellationToken.None), "*/15 * * * *");
RecurringJob.AddOrUpdate<MarketStatusJob>("market-status",   x => x.ExecuteAsync(CancellationToken.None), "*/5 * * * *");
RecurringJob.AddOrUpdate<RecommendationsJob>("recommendations", x => x.ExecuteAsync(CancellationToken.None), "0 6 * * *");
RecurringJob.AddOrUpdate<EarningsJob>("earnings",            x => x.ExecuteAsync(CancellationToken.None), "0 7 * * *");
RecurringJob.AddOrUpdate<EarningsCalendarJob>("earnings-cal",x => x.ExecuteAsync(CancellationToken.None), "0 8 * * *");
RecurringJob.AddOrUpdate<ProfileSyncJob>("profile-sync",     x => x.ExecuteAsync(CancellationToken.None), "0 2 * * 0");
RecurringJob.AddOrUpdate<SymbolCrawlJob>("symbol-crawl",     x => x.ExecuteAsync(CancellationToken.None), "0 2 * * *");
RecurringJob.AddOrUpdate<PollSqsJob>("poll-sqs",             x => x.ExecuteAsync(CancellationToken.None), "*/1 * * * *");
```
