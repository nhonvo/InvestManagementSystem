# Feature Breakdown

> Catalog of all user-facing features in the InventoryAlert UI (v1).

## Page & Feature Map

| Feature | Route | Auth | Description |
|---|---|---|---|
| **Login** | `/login` | Public | JWT authentication form. |
| **Register** | `/register` | Public | New account creation with username + email + password. |
| **Dashboard** | `/dashboard` | ✅ | Overview: portfolio summary, watchlist strip, market status, top news, alert badges. |
| **Portfolio** | `/portfolio` | ✅ | Paginated position list with search + filter. Cost basis, return %, market value. |
| **Position Detail** | `/portfolio/[symbol]` | ✅ | Price chart, trade history, alert rules scoped to this position. |
| **Stocks Catalog** | `/stocks` | ✅ | Browse/search global `StockListing` catalog with exchange + industry filters. |
| **Stock Detail** | `/stocks/[symbol]` | ✅ | Quote, profile, financials, earnings chart, analyst donut, insider table, peers, news. |
| **Watchlist** | `/watchlist` | ✅ | Live watchlist with quick-add via symbol search (DB-first discovery). |
| **Alert Rules** | `/alerts` | ✅ | Full CRUD for alert rules with active/inactive toggle and condition selector. |
| **Market Overview** | `/market` | ✅ | Exchange status grid, news feed, earnings calendar, IPO calendar, holiday list. |

---

## Alert Rule Editor (Key UX Flow)

```mermaid
flowchart LR
    A[Search for stock symbol] --> B{DB or Finnhub resolves it?}
    B -- Yes --> C[Select alert condition]
    B -- No --> Z([Show: Symbol not found])
    C --> D[Set TargetValue + TriggerOnce]
    D --> E[POST /api/v1/alertrules]
    E --> F{Server validates}
    F -- OK --> G[Rule appears in list as Active]
    F -- Error --> H[Show FluentValidation errors]
```

---

## Notification Hub Flow

```mermaid
flowchart LR
    A[SyncPricesJob detects breach] --> B[INSERT Notification row]
    B --> C[UI polls GET /api/v1/notifications/unread-count every 30s]
    C --> D{Count > 0?}
    D -- Yes --> E[Bell badge shows red count]
    E --> F[User opens notification panel]
    F --> G[PATCH /api/v1/notifications/read-all]
    G --> H[Bell clears]
```

---

## Symbol Discovery UX

All flows that require resolving a ticker (portfolio add, watchlist add, alert create) use the same **DB-first + Finnhub fallback** strategy:

1. User types `NVDA` in search modal
2. `GET /api/v1/stocks/search?q=NVDA` → API checks `StockListing` table
3. If not found → calls Finnhub `/search` → persists result
4. UI renders result immediately — user selects and proceeds

---

## Portfolio Cascade Delete

When a user removes a position (`DELETE /api/v1/portfolio/positions/{symbol}`):

| Entity | Action |
|---|---|
| User's `Trade` ledger entries for symbol | ✅ Deleted |
| User's `WatchlistItem` for symbol | ✅ Deleted |
| User's `AlertRule` rows for symbol | ⛔ Blocked (user must delete active rules first — 409) |
| Global `StockListing` | ❌ Not touched |
| Global `PriceHistory` | ❌ Not touched |
