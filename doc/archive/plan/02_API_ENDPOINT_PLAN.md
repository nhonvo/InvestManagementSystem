# 02 — API Endpoint Plan

---

## 1. REST API Overview (InventoryAlert.Api)

Base path: `/api/v1`
Auth: JWT Bearer (all endpoints except `/health`)

---

## 2. Symbols & Watchlist

### `GET /api/v1/symbols/search?q={term}&type=stock|crypto`
Search for a symbol from Finnhub `/search`.
- **Source:** Finnhub API (pass-through with Redis 1-min cache)
- **Returns:** `[{ symbol, description, type, exchange }]`

### `GET /api/v1/symbols/list?exchange={ex}&type=stock|crypto`
Return the pre-crawled list of supported symbols for a given exchange.
- **Source:** PostgreSQL `Products` table (job-populated)
- **Returns:** Paginated list of symbol stubs

### `GET /api/v1/watchlist`
Returns the authenticated user's watchlist with latest cached prices.
- **Source:** PostgreSQL `Watchlists` JOIN `Products`, prices from Redis `quote:{symbol}`

### `POST /api/v1/watchlist/{symbol}`
Add symbol to user watchlist.
- Publishes `symbol.added` event to SNS
- Upserts into PostgreSQL `Products` if first time seen

### `DELETE /api/v1/watchlist/{symbol}`
Remove symbol from user watchlist.
- Publishes `symbol.removed` event to SNS

---

## 3. Stock Data

### `GET /api/v1/stocks/{symbol}/quote`
Real-time price quote.
- **Source:** Redis `quote:{symbol}` (1-min TTL) → fallback Finnhub `/quote`
- **Returns:** `{ price, change, changePercent, high, low, open, prevClose, ts }`

### `GET /api/v1/stocks/{symbol}/profile`
Company profile.
- **Source:** PostgreSQL `CompanyProfiles` (refreshed weekly by job)
- **Returns:** `{ name, logo, industry, marketCap, ipoDate, webUrl, exchange }`

### `GET /api/v1/stocks/{symbol}/news?limit=10&from={date}&to={date}`
Company-specific news.
- **Source:** DynamoDB `inventory-news` (HashKey: TickerSymbol, RangeKey: PublishedAt)
- **Returns:** `[{ headline, summary, source, url, image, publishedAt }]`

### `GET /api/v1/stocks/{symbol}/recommendations`
Latest analyst recommendations.
- **Source:** DynamoDB `inventory-recommendations`
- **Returns:** `[{ period, strongBuy, buy, hold, sell, strongSell }]`

### `GET /api/v1/stocks/{symbol}/earnings?limit=4`
Quarterly earnings history.
- **Source:** DynamoDB `inventory-earnings`
- **Returns:** `[{ period, actual, estimate, surprise, surprisePercent }]`

### `GET /api/v1/stocks/{symbol}/peers`
Peer companies in same sector.
- **Source:** Finnhub `/stock/peers` (cached in Redis 24h)
- **Returns:** `[{ symbol }]`

---

## 4. Market Data

### `GET /api/v1/market/news?category=general|forex|crypto|merger&limit=20`
Latest general market news.
- **Source:** DynamoDB `inventory-market-news`
- **Returns:** `[{ headline, summary, source, url, image, category, publishedAt }]`

### `GET /api/v1/market/status?exchange=US`
Current market open/closed status.
- **Source:** Finnhub `/stock/market-status` (Redis 5-min cache)
- **Returns:** `{ exchange, isOpen, session, holiday }`

### `GET /api/v1/market/holidays?exchange=US`
List of market holidays.
- **Source:** Finnhub `/stock/market-holiday` (Redis 24h cache)
- **Returns:** `[{ atDate, eventName, tradingHour }]`

### `GET /api/v1/market/earnings-calendar?from={date}&to={date}`
Upcoming earnings releases.
- **Source:** Finnhub `/calendar/earnings` (job-cached daily)
- **Returns:** `[{ symbol, date, epsEstimate, revenueEstimate }]`

### `GET /api/v1/market/ipo-calendar?from={date}&to={date}`
Upcoming IPOs.
- **Source:** Finnhub `/calendar/ipo` (job-cached daily)

---

## 5. Crypto

### `GET /api/v1/crypto/exchanges`
List of supported crypto exchanges.
- **Source:** Finnhub `/crypto/exchange` (Redis 1h cache)

### `GET /api/v1/crypto/symbols?exchange={ex}`
List of crypto symbols for given exchange.
- **Source:** Finnhub `/crypto/symbol` (Redis 1h cache)

### `GET /api/v1/crypto/{symbol}/quote`
Quote for a crypto pair (e.g., `BINANCE:BTCUSDT`).
- **Source:** Redis → Finnhub `/quote`

---

## 6. Alert Rules

### `GET /api/v1/alerts`
List all alert rules for authenticated user.
- **Source:** PostgreSQL `AlertRules`

### `POST /api/v1/alerts`
Create a new alert rule.
```json
{
  "symbol": "AAPL",
  "field": "price",
  "operator": "gt",
  "threshold": 200.0,
  "notifyChannel": "telegram | push | email"
}
```
- Publishes `alert.rule.created` to SNS

### `PUT /api/v1/alerts/{ruleId}`
Update alert rule. Publishes `alert.rule.updated`.

### `DELETE /api/v1/alerts/{ruleId}`
Delete rule.

---

## 7. Events (Read-Only Log)

### `GET /api/v1/events?type={eventType}&limit=50`
Query event audit log.
- **Source:** DynamoDB `inventory-event-logs` via `DynamoDbEventLogQuery`

---

## 8. System / Admin

### `GET /health`
Returns service health (no auth required).
- Checks: PostgreSQL, Redis, DynamoDB, SQS/SNS connectivity

### `GET /api/v1/admin/jobs`
Returns Hangfire job status summary (admin role required).

---

## 9. PostgreSQL Table Definitions

### `Products`
```sql
CREATE TABLE "Products" (
    "Id"            SERIAL PRIMARY KEY,
    "TickerSymbol"  VARCHAR(20)  NOT NULL UNIQUE,
    "Name"          VARCHAR(200) NOT NULL,
    "Exchange"      VARCHAR(50)  NOT NULL,
    "Type"          VARCHAR(20)  NOT NULL DEFAULT 'stock', -- stock | crypto
    "CurrentPrice"  DECIMAL(18,4) NULL,
    "LowThreshold"  DECIMAL(18,4) NULL,
    "HighThreshold" DECIMAL(18,4) NULL,
    "CreatedAt"     TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);
```

### `AlertRules`
```sql
CREATE TABLE "AlertRules" (
    "Id"             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId"         VARCHAR(100) NOT NULL,
    "Symbol"         VARCHAR(20)  NOT NULL,
    "Field"          VARCHAR(50)  NOT NULL, -- price | volume | change_pct
    "Operator"       VARCHAR(10)  NOT NULL, -- gt | lt | gte | lte | eq
    "Threshold"      DECIMAL(18,4) NOT NULL,
    "NotifyChannel"  VARCHAR(50)  NOT NULL DEFAULT 'telegram',
    "IsActive"       BOOLEAN      NOT NULL DEFAULT TRUE,
    "LastTriggeredAt" TIMESTAMPTZ NULL,
    "CreatedAt"      TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_alertrules_symbol ON "AlertRules" ("Symbol");
CREATE INDEX idx_alertrules_user   ON "AlertRules" ("UserId");
```

### `CompanyProfiles`
```sql
CREATE TABLE "CompanyProfiles" (
    "Symbol"       VARCHAR(20)   PRIMARY KEY,
    "Name"         VARCHAR(200)  NOT NULL,
    "Logo"         TEXT          NULL,
    "Industry"     VARCHAR(100)  NULL,
    "Exchange"     VARCHAR(50)   NULL,
    "MarketCap"    DECIMAL(20,2) NULL,
    "IpoDate"      DATE          NULL,
    "WebUrl"       TEXT          NULL,
    "Country"      VARCHAR(50)   NULL,
    "Currency"     VARCHAR(10)   NULL,
    "RefreshedAt"  TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);
```

### `Watchlists`
```sql
CREATE TABLE "Watchlists" (
    "Id"        UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId"    VARCHAR(100) NOT NULL,
    "Symbol"    VARCHAR(20)  NOT NULL,
    "AddedAt"   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    UNIQUE ("UserId", "Symbol")
);
CREATE INDEX idx_watchlists_user ON "Watchlists" ("UserId");
```
