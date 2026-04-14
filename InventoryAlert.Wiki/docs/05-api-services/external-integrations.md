# External Service Integrations

> How InventoryAlert communicates with third-party APIs and external services.

---

## Finnhub API

- **Purpose**: Real-time stock quote data, company metadata, and analytics (earnings, recommendations, insiders, peers)
- **Auth**: API key via `?token={API_KEY}` query param (from `FINNHUB_API_KEY` env / `appsettings.json → Finnhub:ApiKey`)
- **Rate Limits**: 60 requests/minute on free tier. Internal cap set at **55 rpm** via Redis counter `finnhub:ratelimit`.

### Endpoints Used

| Finnhub Endpoint | Our Route | Called By |
|---|---|---|
| `GET /api/v1/quote` | `GET /stocks/{symbol}/quote` | `StockDataService` |
| `GET /api/v1/stock/profile2` | `GET /stocks/{symbol}/profile` | `StockDataService` |
| `GET /api/v1/stock/market-status` | `GET /market/status` | `StockDataService` |
| `GET /api/v1/news` | `GET /market/news` | `StockDataService` |
| `GET /api/v1/company-news` | `GET /stocks/{symbol}/news` | `CompanyNewsJob` |
| `GET /api/v1/search` | `GET /stocks/search` | Discovery flow |
| `GET /api/v1/stock/metric` | `GET /stocks/{symbol}/financials` | `SyncMetricsJob` |
| `GET /api/v1/stock/earnings` | `GET /stocks/{symbol}/earnings` | `SyncEarningsJob` |
| `GET /api/v1/stock/recommendation` | `GET /stocks/{symbol}/recommendation` | `SyncRecommendationsJob` |
| `GET /api/v1/stock/insider-transactions` | `GET /stocks/{symbol}/insiders` | `SyncInsidersJob` |
| `GET /api/v1/stock/peers` | `GET /stocks/{symbol}/peers` | `StockDataService` |
| `GET /api/v1/calendar/earnings` | `GET /market/calendar/earnings` | `StockDataService` |
| `GET /api/v1/calendar/ipo` | `GET /market/calendar/ipo` | `StockDataService` |
| `GET /api/v1/stock/market-holiday` | `GET /market/holiday` | `StockDataService` |

### Quote Response Mapping

```json
// Finnhub /quote Response
{
  "c": 172.50,    // currentPrice
  "d": -2.30,     // change
  "dp": -1.32,    // changePercent
  "h": 175.00,    // high
  "l": 170.10,    // low
  "o": 173.00,    // open
  "pc": 174.80,   // prevClose
  "t": 1713000000 // timestamp
}
```

### Error Handling Rules

- If `c` (current price) is `null` or `0` → **skip** that symbol, log warning, continue loop.
- If Finnhub returns an HTTP error → **log and skip**, never throw.
- Quotes are cached in Redis for **30 seconds** (`quote:{symbol}`).

```csharp
// IFinnhubClient contract
if (quote?.CurrentPrice is null or 0) return null; // skip — free tier limitation
```

---

## Amazon SQS

- **Purpose**: Async event bus between `InventoryAlert.Api` / `InventoryAlert.Worker`
- **Local Emulation**: [Moto](https://github.com/getmoto/moto) running in Docker at `http://moto:5000`
- **Configuration**: `Aws:SqsQueueUrl` in `appsettings.Docker.json`

### Queues

| Queue | Purpose | Publisher | Consumer |
|---|---|---|---|
| `inventory-events` | Domain event bus | Api, Worker | Worker (`IntegrationMessageRouter`) |
| `inventory-events-dlq` | Dead letter queue | SQS (auto-redrive after 5 failures) | Manual replay via Hangfire / AWS Console |

### Consumer Pattern

```csharp
// ProcessQueueJob — long-polling
var messages = await _sqsHelper.ReceiveMessagesAsync(queueUrl, maxMessages: 10, ct: ct);
```

- **Visibility Timeout**: 30 seconds (message hidden from other consumers while processing)
- **Max Receive Count**: 5 — after 5 failures, message is moved to DLQ automatically
- **Long-polling Wait**: 20 seconds per poll cycle to reduce empty-response API calls

---

## Amazon DynamoDB

- **Purpose**: Permanent storage for high-volume news data (never deleted, queryable by range)
- **Local Emulation**: Same Moto container as SQS (`http://moto:5000`)

### Tables

| Table | PK | SK | GSI |
|---|---|---|---|
| `inventoryalert-market-news` | `CATEGORY#<category>` | `TS#<unix_ms>` | — |
| `inventoryalert-company-news` | `SYMBOL#<ticker>` | `TS#<unix_ms>` | `BySymbolAndDate` (Symbol, SK) |

### Access Patterns

- **Write**: `PutItemAsync` on news fetch. Deduplication by `NewsId` attribute before insert.
- **Read (Market News)**: `QueryAsync` with PK `CATEGORY#general`, SK range from `TS#{30_days_ago}` to `TS#{now}`.
- **Read (Company News)**: `QueryAsync` with PK `SYMBOL#TSLA`, sorted descending, limit 20.

> **No TTL**: News is retained indefinitely as a historical archive. Use SK range filters to retrieve recent articles.

---

## Redis

- **Purpose**: Quote caching, SQS deduplication, alert cooldown, Finnhub rate limiting
- **Local**: `localhost:6379` (Docker: `inventory-cache`)
- **Namespaces**:

| Key | TTL | Purpose |
|---|---|---|
| `quote:{symbol}` | 30s | Cached Finnhub quote |
| `dedup:sqs:{messageId}` | 30 min | SQS exactly-once guard |
| `cooldown:alert:{symbol}` | 24h | Prevent alert storms |
| `finnhub:ratelimit` | Rolling 60s | Rate limit counter (cap 55 rpm) |
