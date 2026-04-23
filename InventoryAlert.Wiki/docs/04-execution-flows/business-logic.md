# Business Logic & Rules (v1)

This document outlines the core domain logic governing the InventoryAlert.Api system.

---

## 1. Alert Evaluation Logic

### 1.1 Price Alerts

Evaluated during the **price sync loop** inside `SyncPricesJob` (every 15 min). Loads all active `AlertRule` rows for each ticker, compares against the latest quote.

| Condition | Evaluation |
|---|---|
| `PriceAbove` | `quote.CurrentPrice > rule.TargetValue` |
| `PriceBelow` | `quote.CurrentPrice < rule.TargetValue` |
| `PriceTargetReached` | `|quote.CurrentPrice - rule.TargetValue| < tolerance` |

On breach:
- An unread `Notification` row is written for the owning user.
- `rule.LastTriggeredAt` is updated.
- If `rule.TriggerOnce = true`, the rule is deactivated (`IsActive = false`).
- Redis cooldown key `cooldown:alert:{symbol}` is set (24h TTL) to prevent alert storms.

### 1.2 Portfolio Cost-Basis Alerts (`PercentDropFromCost`)

Evaluates an individual user's specific unrealized loss exposure.

- **Trigger**: inside `SyncPricesJob`, if `rule.Condition == PercentDropFromCost`.
- **Formula**: `(avgCostBasis - currentPrice) / avgCostBasis * 100 >= rule.TargetValue`
- **Cost Basis**: Computed by joining `Trade` where `UserId = rule.UserId AND TickerSymbol = rule.TickerSymbol`, type = `Buy`.
- **User Isolation**: This query is ALWAYS scoped to a single `(UserId, TickerSymbol)` pair. Never aggregated across users.

### 1.3 Low Holdings Alert (`LowHoldingsCount`)

Triggered synchronously by `LowHoldingsHandler` when a trade is executed — not on the 15-minute cycle.

- **Formula**: `SUM(Quantity WHERE Type = Buy) - SUM(Quantity WHERE Type = Sell) < rule.TargetValue`
- **Guard**: The service guards against net holdings going negative (oversell) — rejects the trade with `422 Unprocessable`.

---

## 2. Trade Audit Ledger

Every holding change via `POST/PATCH /portfolio` is recorded as an **immutable `Trade` row**.

| Field | Description |
|---|---|
| `UserId` | Owner context — never cross-user |
| `TickerSymbol` | Market ticker |
| `Type` | `Buy`, `Sell`, `Dividend`, `Split` |
| `Quantity` | **Always positive**. Direction is encoded by `Type`. |
| `UnitPrice` | Cost per share at execution. `0` for `Dividend`/`Split`. |
| `TradedAt` | UTC execution timestamp |
| `Notes` | Optional annotation (max 500 chars) |

Net holdings = `SUM(Buy) - SUM(Sell)` — computed dynamically by `TradeRepository.GetNetHoldingsAsync`.

---

## 3. Symbol Discovery (DB-First + Finnhub Fallback)

Symbol resolution applies to **every flow that requires a ticker**: search, portfolio add, watchlist add, alert create.

```
Client request with symbol/query
    ↓
DB: SELECT FROM StockListing WHERE TickerSymbol = ? (exact) or ILIKE ? (search)
    ↓ Found → return immediately
    ↓ Not Found →
        Finnhub: GET /search or /stock/profile2
            ↓ Not found → 404 Symbol not recognized
            ↓ Found →
                DB: INSERT StockListing (ON CONFLICT DO NOTHING)
                → Return result to caller
                → Background: SyncMetricsJob enqueued for new symbol
```

**Rule**: Finnhub is called at most once per symbol. After that, all users benefit from the local cache permanently.

---

## 4. Portfolio Cascade Delete

When `DELETE /portfolio/positions/{symbol}` is called:

| Step | Action |
|---|---|
| 1. Guard | If user has active `AlertRule` for this symbol → return `409 Conflict`. User must delete rules first. |
| 2. Cascade | Delete user's `Trade` rows for this symbol |
| 3. Cascade | Delete user's `WatchlistItem` for this symbol |
| 4. Preserve | Keep `StockListing`, `PriceHistory`, `StockMetric`, `EarningsSurprise`, `RecommendationTrend`, `InsiderTransaction` — these are global market data used by all users |

---

## 5. Market Intelligence Sync Schedule

| Data | Job | Frequency | Finnhub Endpoint |
|---|---|---|---|
| Price quotes | `SyncPricesJob` | Every 15 min | `/quote` |
| Basic Financials | `SyncMetricsJob` | Daily 06:00 UTC | `/stock/metric` |
| Earnings Surprises | `SyncEarningsJob` | Daily 07:00 UTC | `/stock/earnings` |
| Analyst Recommendations | `SyncRecommendationsJob` | Weekly (Monday) | `/stock/recommendation` |
| Insider Transactions | `SyncInsidersJob` | Daily 08:00 UTC | `/stock/insider-transactions` |
| Company News | `CompanyNewsJob` | Every 6h | `/company-news` |
| Market News | `MarketNewsJob` | Every 2h | `/news` |
| Price History Cleanup | `CleanupPriceHistoryJob` | Daily 00:00 UTC | — (DB delete) |

---

## 6. Validation Rules (FluentValidation)

Applied at the Web layer only. The Application layer trusts pre-validated inputs.

| DTO | Key Rules |
|---|---|
| `LoginRequest` | Username `NotEmpty MaxLength(50)`, Password `MinLength(6) MaxLength(100)` |
| `RegisterRequest` | Username `Matches(^[a-zA-Z0-9_]+$)`, Password min 1 uppercase + 1 digit + 1 special char |
| `CreatePositionRequest` | TickerSymbol `Matches(^[A-Z0-9.]+$)`, Quantity `> 0`, UnitPrice `> 0 && < 1_000_000`, TradedAt `<= UtcNow` |
| `TradeRequest` | Type must be valid enum; Quantity `> 0`; UnitPrice `> 0` (except Dividend/Split) |
| `AlertRuleRequest` | PercentDropFromCost: TargetValue `0.01–100`; LowHoldingsCount: whole number only |
