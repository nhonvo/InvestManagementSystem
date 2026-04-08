# 05 — UI Plan (Next.js)

---

## 1. Tech Stack

| Concern         | Choice                                   |
|-----------------|------------------------------------------|
| Framework       | Next.js 15 (App Router)                  |
| Styling         | Tailwind CSS + shadcn/ui                 |
| State           | Zustand (client) + React Query (server)  |
| Charts          | Recharts                                 |
| Real-time       | WebSocket or SSE (via API proxy)         |
| Auth            | NextAuth.js (JWT, provider-flexible)     |
| Deployment      | Docker container (nginx)                 |

---

## 2. Pages & Components

### `/` — Dashboard (Watchlist)
- Header: market status badge (open/closed), current time
- **WatchlistGrid**: cards per symbol
  - Symbol ticker, company name, logo
  - Current price + % change (color-coded)
  - Sparkline (last 24h from `inventory-price-history`)
  - "Alert active" indicator
- **MarketNewsFeed**: right panel, latest 10 general news items
- Auto-refresh every 60s via React Query `refetchInterval`

### `/stocks/[symbol]` — Symbol Detail Page
Tabs:
1. **Overview** — Profile card (logo, name, industry, market cap, IPO date, website)
2. **Price** — Line chart (1D / 1W / 1M / 3M from DynamoDB price history) + current quote card
3. **News** — Paginated company news feed with thumbnail, headline, source, date
4. **Recommendations** — Stacked bar by period (StrongBuy / Buy / Hold / Sell / StrongSell)
5. **Earnings** — Bar chart: Actual vs Estimate EPS per quarter + surprise % line

### `/market` — Market Overview
- Market status card (US, EU, APAC)
- Market news feed (all categories with filter)
- Upcoming earnings calendar (7-day window)
- Upcoming IPO calendar

### `/alerts` — Alert Rule Manager
- Table of user's active rules
- Create rule form: `{symbol} {field} {operator} {threshold} → {channel}`
- Rule history: last triggered at, trigger count
- Toggle rule active/inactive

### `/admin/events` — Event Log Viewer
- Filterable table: EventType, Status, date range
- Drill-down to full payload JSON
- Error events highlighted in red

### `/admin/health` — System Health
- Service status cards: API, Worker, PostgreSQL, Redis, DynamoDB, SQS/SNS
- Hangfire job status timeline (last 24h)
- Log level distribution chart (Info / Warn / Error per hour)

---

## 3. API Integration (from UI)

The Next.js app calls the `InventoryAlert.Api` via server-side Route Handlers to avoid CORS:

```
Browser → Next.js Server (Route Handler)
                → fetch("http://api:8080/api/v1/...")
                  → returns JSON to browser
```

All sensitive tokens (JWT, API host) stay server-side. Client sees only UI data.

---

## 4. Key UI Components

```
components/
  watchlist/
    WatchlistGrid.tsx       # Grid of SymbolCard tiles
    SymbolCard.tsx          # Price + sparkline + change badge
  stocks/
    PriceChart.tsx          # Recharts LineChart with range selector
    NewsCard.tsx            # Headline, source, thumbnail, date
    RecommendationChart.tsx # Stacked bar chart
    EarningsChart.tsx       # Bar + line combo chart
  market/
    MarketStatusBadge.tsx   # Open/Closed with exchange label
    NewsItem.tsx            # Market news list item
    EarningsCalendar.tsx    # Table of upcoming earnings
  alerts/
    AlertRuleForm.tsx       # Create/edit rule form
    AlertRuleTable.tsx      # CRUD table
  admin/
    EventLogTable.tsx       # Filterable event log
    HealthCard.tsx          # Service status card
  ui/
    Sparkline.tsx           # Tiny inline chart
    PriceChip.tsx           # Price + change with color
    Badge.tsx               # Generic status badge
```

---

## 5. Real-time Updates

Option A (simpler — polling):
- React Query `refetchInterval: 60_000` for prices
- `refetchInterval: 300_000` for news

Option B (preferred for prices):
- API exposes `GET /api/v1/stocks/{symbol}/quote/stream` as SSE
- Worker pushes `stock.price.updated` → Redis Pub/Sub → API SSE clients
- Client `EventSource` receives update and triggers React Query cache invalidation

---

## 6. Docker Service

```yaml
# docker-compose additions
ui:
  build:
    context: ./InventoryAlert.UI
  ports:
    - "3000:3000"
  environment:
    - NEXT_PUBLIC_API_URL=http://localhost:8080
    - NEXTAUTH_SECRET=dev-secret
    - NEXTAUTH_URL=http://localhost:3000
  depends_on:
    - api
```
