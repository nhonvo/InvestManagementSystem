# Frontend Architecture

> How the Next.js 15 UI application is structured, fetches data, and manages state against the v2 API.

## App Router Structure

```
InventoryAlert.UI/
├── src/
│   ├── app/
│   │   ├── (auth)/
│   │   │   ├── login/            ← JWT authentication form
│   │   │   └── register/         ← New account creation
│   │   ├── page.tsx              ← Dashboard: Portfolio summary, watchlist strip, market status, news (Route: /)
│   │   ├── portfolio/            ← Paginated position list with trade history
│   │   │   └── [symbol]/         ← Position detail: chart, trades, alerts
│   │   ├── stocks/               ← Global StockListing catalog (browse + search)
│   │   │   └── [symbol]/         ← Stock detail: quote, profile, financials, earnings, peers
│   │   ├── watchlist/            ← Live watchlist with quick-add
│   │   ├── alerts/               ← Alert rule CRUD with toggle
│   │   ├── market/               ← Exchange status, news feed, earnings calendar, IPO calendar
│   │   ├── admin/                ← Admin only pages
│   │   │   └── health/           ← Real-time system health dashboard
│   │   └── layout.tsx            ← Root layout with Navbar, Sidebar, MarketStatusBanner
│   ├── components/               ← Reusable UI components
│   ├── hooks/                    ← Custom React hooks (useQuote, useNotifications)
│   └── lib/                      ← API client (api.ts), auth helpers
```

---

## Page → API Map

| Page | Route | Key API Calls |
|---|---|---|
| Dashboard | `/` | `GET /portfolio/positions`, `GET /watchlist/`, `GET /market/status`, `GET /market/news` |
| Portfolio | `/portfolio` | `GET /portfolio/positions` (paged), `GET /portfolio/alerts` |
| Position Detail | `/portfolio/[symbol]` | `GET /portfolio/positions/{symbol}`, `GET /stocks/{symbol}/quote`, `GET /alertrules/` |
| Stock Catalog | `/stocks` | `GET /stocks/` (paged), `GET /stocks/search` |
| Stock Detail | `/stocks/[symbol]` | `GET /stocks/{symbol}/quote`, `/profile`, `/financials`, `/earnings`, `/recommendation`, `/insiders`, `/peers`, `/news` |
| Watchlist | `/watchlist` | `GET /watchlist/`, `POST /watchlist/{symbol}`, `DELETE /watchlist/{symbol}` |
| Alerts | `/alerts` | `GET /alertrules/`, `POST /alertrules/`, `PUT /alertrules/{id}`, `PATCH /alertrules/{id}/toggle` |
| Market | `/market` | `GET /market/status`, `/news`, `/calendar/earnings`, `/calendar/ipo`, `/holiday` |
| Admin Health | `/admin/health` | `GET /health` (via proxy or direct) |

---

## Component Library

### Navigation

| Component | Description |
|---|---|
| `Navbar` | Top bar: logo, nav links, user avatar menu, notification bell (polls `/notifications/unread-count` every 30s), GlobalSearch trigger |
| `Sidebar` | Collapsible left nav: Dashboard, Portfolio, Watchlist, Stocks, Alerts, Market |
| `MarketStatusBanner` | Color-coded strip showing exchange name + open/closed state. Polls `GET /market/status` every 60s |
| `GlobalSearch` | Cmd+K modal. Debounce 300ms. Uses DB-first discovery flow via `GET /stocks/search` |

### Market Data Widgets

| Component | API | Description |
|---|---|---|
| `StockQuoteCard` | `GET /stocks/{symbol}/quote` | Price, change, change%, high, low. Refreshes every 30s |
| `PriceLineChart` | Local `PriceHistory` data | Recharts `AreaChart`. 1D/1W/1M/3M/1Y range selector |
| `Market News Sync` | `POST /events` | CQRS Command to trigger a Finnhub background sync. Issues a `SyncMarketNewsRequested` event. |
| `EarningsBarChart` | `GET /stocks/{symbol}/earnings` | Grouped bar: actual vs. estimate EPS per quarter |
| `RecommendationDonut` | `GET /stocks/{symbol}/recommendation` | Recharts `PieChart` — Strong Buy/Buy/Hold/Sell/Strong Sell |
| `MetricsPanel` | `GET /stocks/{symbol}/financials` | P/E, P/B, EPS, Dividend Yield, 52w High/Low, Revenue Growth, Net Margin |
| `InsiderTable` | `GET /stocks/{symbol}/insiders` | Sortable: Name, Date, Shares, Value, Code badge |
| `PeersChipRow` | `GET /stocks/{symbol}/peers` | Clickable symbol chips → navigates to `/stocks/[symbol]` |

### Portfolio Components

| Component | API | Description |
|---|---|---|
| `PositionTable` | `GET /portfolio/positions` | Paginated, searchable holdings table |
| `TradeModal` | `POST /portfolio/{symbol}/trades` | Form: Type, Quantity, Unit Price, Notes, Date |
| `AddPositionModal` | `POST /portfolio/positions` | Symbol discovery → quantity/price → confirm |
| `BulkImportModal` | `POST /portfolio/bulk` | CSV drag-and-drop → preview → confirm with error summary |
| `AlertBadge` | `GET /portfolio/alerts` | Red badge on positions that breached an `AlertRule` |

### Alert Rule Components

| Component | API | Description |
|---|---|---|
| `AlertRuleTable` | `GET /alertrules/` | Full list: Symbol, Condition, Target, Status, Last Triggered, Actions |
| `AlertRuleForm` | `POST/PUT /alertrules/` | Symbol input (search optional) → Condition dropdown → Target value → TriggerOnce |
| `AlertToggle` | `PATCH /alertrules/{id}/toggle` | Inline toggle switch with optimistic UI |

---

## State Management

| Concern | Tool | Rationale |
|---|---|---|
| Server data (quotes, portfolio, news) | **React Query** | Auto-refresh, stale-while-revalidate, cache invalidation on mutations |
| UI state (modals, tabs, filters) | **Zustand** | Lightweight, no boilerplate, easy DevTools |
| Auth token | `httpOnly` cookie | Secure; never exposed to `localStorage` |
| Optimistic updates | React Query `onMutate` | Alert toggles, watchlist add/remove — feels instant |
| Quote polling | `refetchInterval: 30_000` | Active only when tab is visible (`refetchIntervalInBackground: false`) |
| Notification badge | `refetchInterval: 30_000` | Polls `/notifications/unread-count` |

---

## Auth Flow in UI

The JWT access token is delivered via JSON body (stored in React Query state). The `httpOnly` refresh token cookie is set server-side by the API on login. The `middleware.ts` file protects all routes under `/dashboard`, `/portfolio`, `/watchlist`, `/stocks`, `/alerts`, and `/market` — redirecting unauthenticated users to `/login`.
