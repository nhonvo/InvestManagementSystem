---
description: Business capabilities, REST API reference, and workflows for the Inventory Alert System.
type: reference
status: active
version: 2.0
tags: [business, features, api, workflows, reference]
last_updated: 2026-04-05
---

# 🛒 Inventory Alert System — Features & API Reference

> This document covers **business capabilities, API endpoints, and user-facing workflows**.
> For system architecture, Docker setup, and project internals, see [`PROJECT_OVERVIEW.md`](PROJECT_OVERVIEW.md).

---

## 🔐 0. Authentication

All `ProductsController` endpoints require a valid **JWT Bearer token**.

### Login
```
POST /api/auth/login
Body: { "username": "admin", "password": "admin123" }
→ 200 OK: { "token": "<jwt>" }
→ 401 Unauthorized
```

- Tokens are **HmacSha256-signed**, expire after **2 hours**
- Include in subsequent requests: `Authorization: Bearer <token>`
- Credentials configured in `appsettings.json → Auth:Username / Auth:Password`
- `EventsController` endpoints are **unauthenticated** (internal/service-to-service)

---

## 📦 1. Product Inventory Management

**Business Goal:** Centralized catalog for warehouse staff to manage product metadata, pricing baselines, and stock thresholds.

### The `Product` Entity

| Field | Type | Purpose |
|---|---|---|
| `Id` | `int` | Primary key |
| `Name` | `string` | Human-readable product name |
| `TickerSymbol` | `string` | NYSE/NASDAQ ticker (e.g. `AAPL`) — links to Finnhub |
| `OriginPrice` | `decimal` | Baseline purchase price — **never changes automatically** |
| `CurrentPrice` | `decimal` | Latest price synced from Finnhub |
| `PriceAlertThreshold` | `double` | e.g. `0.2` = alert when price drops >20% from origin |
| `StockCount` | `int` | Current physical unit count |
| `StockAlertThreshold` | `int` | Alert threshold for low-stock events |
| `LastAlertSentAt` | `DateTime?` | Cooldown gate — prevents duplicate alert spam |

### Endpoints

| Method | Route | Handler | Response |
|---|---|---|---|
| `GET` | `/api/products` | `GetProducts([FromQuery] PaginationParams)` | `200 PagedResult<ProductResponse>` |
| `GET` | `/api/products/{id}` | `GetProductById(int id)` | `200 ProductResponse` / `404` |
| `POST` | `/api/products` | `CreateProduct(ProductRequest)` | `201 ProductResponse` |
| `PUT` | `/api/products/{id}` | `UpdateProduct(int id, ProductRequest)` | `200 ProductResponse` / `404` |
| `PATCH` | `/api/products/{id}/stock?stockCount={n}` | `UpdateStockCount(int id, int stockCount)` | `200 ProductResponse` / `404` |
| `DELETE` | `/api/products/{id}` | `DeleteProduct(int id)` | `204 No Content` / `404` |
| `POST` | `/api/products/bulk` | `BulkInsertProducts(IEnumerable<ProductRequest>)` | `204 No Content` |
| `GET` | `/api/products/price-alerts` | `GetPriceLossAlerts()` | `200 IEnumerable<PriceLossResponse>` |
| `POST` | `/api/products/sync-price` | `SyncStockPrice()` | `204 No Content` |

### Pagination
`GET /api/products` accepts `?pageNumber=1&pageSize=20` and returns:
```json
{
  "items": [...],
  "totalItems": 150,
  "pageNumber": 1,
  "pageSize": 20
}
```

### Single-Item Cache
`GetProductByIdAsync` uses **`IMemoryCache`** with key `"Product_{id}"` and a **10-minute sliding TTL**.
`UpdateProductAsync`, `UpdateStockCountAsync`, and `DeleteProductAsync` all call `_cache.Remove($"Product_{id}")` to invalidate immediately.

### Bulk Insert
`POST /api/products/bulk` accepts a JSON array of `ProductRequest`. Runs the entire insert inside `ExecuteTransactionAsync` — all-or-nothing. Returns `204` with no body to minimize bandwidth on large payloads.

---

## 📉 2. Stock Count Management

**Business Goal:** Allow warehouse scanning devices to update unit counts without overwriting other concurrent edits.

### Surgical PATCH Pattern
`PATCH /api/products/{id}/stock?stockCount=42` only updates the `StockCount` field.  
The full `Product` record is re-fetched first (`GetByIdAsync`), then only `StockCount` is mutated before the transaction write — preventing race conditions where a scanner zeroes out a field it didn't intend to touch.

```
PATCH /api/products/7/stock?stockCount=42
Authorization: Bearer <token>
→ 200: { "id": 7, "name": "iPhone 15", "stockCount": 42, ... }
→ 404: if product doesn't exist
```

---

## 📈 3. Live Price Sync (Finnhub Integration)

**Business Goal:** Keep `CurrentPrice` aligned with real-world market valuations to drive accurate loss alerts.

### Automatic Background Sync

`SyncPricesJob` (Hangfire, scheduled inside `InventoryAlert.Worker`) periodically calls `IProductService.SyncCurrentPricesAsync`:

1. Fetches all products from `IProductRepository`
2. For each product, calls `IFinnhubClient.GetQuoteAsync(tickerSymbol, ct)`
3. If `quote.CurrentPrice is null or 0` → logs a warning and **skips** (never throws)
4. Builds a batch of updated `Product` objects
5. Wraps `UpdateRangeAsync` inside `ExecuteTransactionAsync` — all-or-nothing bulk update

### Manual Trigger

```
POST /api/products/sync-price
Authorization: Bearer <token>
→ 204 No Content (after sync completes)
```

Useful for analysts to force an immediate refresh before running price-alert queries.

---

## 🚨 4. Price Loss Alert Engine

**Business Goal:** Automatically detect and report products whose market price has dropped dangerously below the baseline cost.

### Alert Calculation

`GetPriceLossAlertsAsync` iterates all products and calculates:
```
priceDiff          = CurrentPrice - OriginPrice
priceChangePercent = priceDiff / OriginPrice  (negative = loss)
lossMagnitude      = Math.Abs(priceChangePercent)

→ Alert fires if: lossMagnitude >= PriceAlertThreshold
```

### Cooldown Gate (Spam Prevention)
Before raising an alert, the system checks `LastAlertSentAt`:
```csharp
var cooldown = TimeSpan.FromHours(1);
if (product.LastAlertSentAt.HasValue &&
    DateTime.UtcNow - product.LastAlertSentAt.Value < cooldown)
    continue; // skip — still in cooldown window
```
This prevents the same product from generating duplicate notifications within 60 minutes, even if the price remains below threshold.

### Alert Endpoint

```
GET /api/products/price-alerts
Authorization: Bearer <token>
→ 200: [
    {
      "id": 3,
      "name": "Tesla Model S Parts",
      "tickerSymbol": "TSLA",
      "originPrice": 950.00,
      "currentPrice": 700.00,
      "priceDiff": 250.00,
      "priceChangePercent": -0.2632,
      "priceAlertThreshold": 0.2,
      "stockCount": 15
    }
  ]
```

---

## 📨 5. Event Publishing & Audit

**Business Goal:** Emit structured domain events to downstream services (Telegram, Slack, analytics) without coupling to the REST API.

### Publish Any Event

```
POST /api/events
Body: { "eventType": "inventoryalert.pricing.price-drop.v1", "payload": { ... } }
→ 202 Accepted
```

### Trigger Pre-Built Alerts

| Endpoint | Event Type | Payload |
|---|---|---|
| `POST /api/events/market-alert` | `MarketPriceAlert` | `{ productId, symbol, originPrice, currentPrice, dropPercent }` |
| `POST /api/events/news-alert` | `CompanyNewsAlert` | `{ symbol, headline, source, url, publishedAt }` |

### Event Type Registry

```
GET /api/events/types
→ 200: [
    "inventoryalert.pricing.price-drop.v1",
    "inventoryalert.inventory.stock-low.v1",
    "inventoryalert.fundamentals.earnings.v1",
    "inventoryalert.fundamentals.insider-sell.v1",
    "inventoryalert.news.headline.v1"
  ]
```

### Audit Log (DynamoDB)

```
GET /api/events/logs/{eventType}?limit=20
→ 200: [ { "messageId": "...", "eventType": "...", "timestamp": "...", ... } ]
```

Every published event is written to **DynamoDB `EventLogs` table** before SNS dispatch. Logs are query-able by `eventType` with a configurable `limit`.

---

## 🔁 6. Background Event Handling (Worker)

**Business Goal:** Process incoming domain events asynchronously without blocking the API, with persistence and notification side effects.

### Registered Event Handlers (Worker)

| Event Type | Handler | Side Effect |
|---|---|---|
| `MarketPriceAlert` | `PriceAlertHandler` | Structured log: `🚨 PRICE DROP ALERT {symbol} -{dropPercent:P1}` — **Telegram plug-in point** |
| `EarningsAlert` | `EarningsHandler` | Persists `EarningsRecord` to PostgreSQL (EPS actual vs estimated) |
| `CompanyNewsAlert` | `NewsHandler` | Persists `NewsRecord` to PostgreSQL (headline, source, URL) |
| `InsiderSellAlert` | `InsiderHandler` | Persists `InsiderTransaction` to PostgreSQL |
| *(unknown type)* | `UnknownEventHandler` | Safely logs and discards — no crash, no DLQ escalation |

### Dead-Letter Queue
After 3 failed processing attempts, SQS routes the message to `inventory-event-dlq` for manual inspection without causing consumer stalls.
