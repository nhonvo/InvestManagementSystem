---
description: InventoryAlert.UI audit of pages/components and recommendations (paging, reuse, UX, maintainability). Documentation only.
type: reference
status: draft
version: 1.0
tags: [ui, audit, components, pages, ux, nextjs, inventoryalert]
last_updated: 2026-04-26
---

# InventoryAlert.UI — Components & Pages Audit + Enhancement Suggestions

This document scans the current UI project and lists:

- every route/page and what it calls,
- reusable components and where they are used,
- gaps and recommended enhancements (especially paging, consistency, and maintainability).

Scope:

- UI code: `InventoryAlert.UI/src/*`
- No code changes included here (documentation-only).

Files scanned (as of `2026-04-26`):

- Routes: `InventoryAlert.UI/src/app/**/page.tsx`, `InventoryAlert.UI/src/app/layout.tsx`
- Components: `InventoryAlert.UI/src/components/*.tsx`
- Hooks: `InventoryAlert.UI/src/hooks/*.ts`
- Client utilities: `InventoryAlert.UI/src/lib/*.ts`

---

## 1) Project Entry + Global Flow

### 1.1 Root layout + providers

`InventoryAlert.UI/src/app/layout.tsx`:

- applies theme class early (inline script)
- wraps the app with:
  - `ThemeProvider` (`src/components/ThemeProvider.tsx`)
  - `NotificationProvider` (`src/components/NotificationProvider.tsx`)
  - `NavbarWrapper` (`src/components/NavbarWrapper.tsx`)

Render tree (simplified):

`<ThemeProvider>` → `<NotificationProvider>` → `<NavbarWrapper>` → `<main>{children}</main>`

### 1.2 API client pattern

`InventoryAlert.UI/src/lib/api.ts`:

- reads access token from `localStorage` (`auth_token`)
- attaches `Authorization: Bearer <token>` when present
- always sends cookies (`credentials: "include"`) to support refresh token flow
- on `401`:
  - attempts refresh via `POST /api/v1/auth/refresh`
  - retries once
  - evicts token and redirects to `/login` if refresh fails

Enhancement notes:

- error parsing currently checks `userFriendlyMessage/message/title`, but the API error shape is `ErrorResponse { errors[], errorId }`. Consider mapping UI error extraction to `errors[0].message` (and include `X-Correlation-Id` when present).

---

## 2) Pages (Routes) Inventory

All routes are App Router pages under `InventoryAlert.UI/src/app/*`.

### `/` — Dashboard

File: `InventoryAlert.UI/src/app/page.tsx`

Fetches on mount:

- `GET /api/v1/watchlist`
- `GET /api/v1/market/news?category=general&page=1`
- `GET /api/v1/portfolio/positions`

Actions:

- Enqueue market news refresh:
  - `POST /api/v1/events` with `eventType=inventoryalert.news.sync-requested.v1`
- Watchlist remove:
  - `DELETE /api/v1/watchlist/{symbol}`

Enhancement ideas:

- Add paging controls for news (or link to Market page with selected category).
- Centralize toast/confirm patterns (this page already uses `Toast` + `ConfirmDialog`).
- Consider refetch strategy (manual refresh button, or background refresh interval with backoff).

### `/market` — Market Pulse

File: `InventoryAlert.UI/src/app/market/page.tsx`

Fetches (category/page-driven):

- `GET /api/v1/market/news?category={cat}&page={newsPage}&pageSize=10`
- `GET /api/v1/market/status`
- `GET /api/v1/market/calendar/earnings?from=…&to=…` (errors swallowed to `[]`)

Actions:

- Enqueue market news refresh via `POST /api/v1/events`

Enhancement ideas:

- Add explicit paging UI for `newsPage` (next/prev) and show current page.
- Add a “loading/error state” component instead of `console.error`.
- Use `MarketStatusBanner` here (currently status is fetched and displayed in-page).

### `/stocks` — Stock Catalog

File: `InventoryAlert.UI/src/app/stocks/page.tsx`

Fetches:

- browse: `GET /api/v1/stocks?page={page}&pageSize=20`
- search: `GET /api/v1/stocks/search?q={query}`

Enhancement ideas:

- Add page size selector and show total results if API returns it.
- Add “clear search” UX and keyboard navigation to results (GlobalSearch already has it).

### `/stocks/[symbol]` — Stock Detail

File: `InventoryAlert.UI/src/app/stocks/[symbol]/page.tsx`

Eager fetches on symbol change:

- `GET /api/v1/stocks/{symbol}/quote`
- `GET /api/v1/stocks/{symbol}/profile`
- `GET /api/v1/stocks/{symbol}/financials` (errors swallowed to `null`)
- `GET /api/v1/watchlist` (to determine watchlist status; client-side check)

Lazy per tab:

- Earnings: `GET /api/v1/stocks/{symbol}/earnings` (with loaded/loading/error state)
- Recommendations: `GET /api/v1/stocks/{symbol}/recommendation`
- Insiders: `GET /api/v1/stocks/{symbol}/insiders`
- News: `GET /api/v1/stocks/{symbol}/news?page={newsPage}&pageSize=10`
- Peers: `GET /api/v1/stocks/{symbol}/peers`

Actions:

- Watchlist toggle:
  - `POST /api/v1/watchlist/{symbol}` or `DELETE /api/v1/watchlist/{symbol}`
- Price alert modal:
  - `POST /api/v1/alertrules` (via `PriceAlertModal`)

Enhancement ideas:

- Add visible paging controls for the News tab (`newsPage` exists in state).
- Replace `alert()` error UX with `Toast`.
- Avoid fetching full watchlist list just to check one symbol (add a backend endpoint `GET /watchlist/{symbol}/exists` or cache watchlist in a provider).
- Add per-tab skeletons and per-tab error UI (only earnings has a dedicated error state today).

### `/portfolio` — Portfolio

File: `InventoryAlert.UI/src/app/portfolio/page.tsx`

Fetches:

- `GET /api/v1/portfolio/positions` (expects `PagedResult`, but does not pass page params)

Actions:

- Delete position:
  - `DELETE /api/v1/portfolio/positions/{symbol}`
- Create/adjust position and trades via `TradeModal`:
  - `POST /api/v1/portfolio/positions`
  - `POST /api/v1/portfolio/{symbol}/trades`

Enhancement ideas:

- Implement paging UI if portfolios can grow (API already returns paged result).
- Provide optimistic UI updates for trade/position changes.

### `/watchlist` — Watchlist

File: `InventoryAlert.UI/src/app/watchlist/page.tsx`

Fetches:

- `GET /api/v1/watchlist`

Actions:

- Add symbol:
  - `POST /api/v1/watchlist/{symbol}` (via `AddSymbolModal`)
- Remove symbol:
  - `DELETE /api/v1/watchlist/{symbol}`

Enhancement ideas:

- Add sorting (price, change %, alpha).
- Add “bulk remove” mode for large lists (with ConfirmDialog).

### `/alerts` — Alert Rules Manager

File: `InventoryAlert.UI/src/app/alerts/page.tsx`

Fetches:

- `GET /api/v1/alertrules`

Actions:

- Create:
  - `POST /api/v1/alertrules`
- Update:
  - `PUT /api/v1/alertrules/{id}`
- Toggle:
  - `PATCH /api/v1/alertrules/{id}/toggle`
- Delete:
  - `DELETE /api/v1/alertrules/{id}` (currently uses `confirm(...)`)

Enhancement ideas:

- Replace native `confirm()` and `alert()` with `ConfirmDialog` + `Toast` for consistent UX.
- Add filtering tabs: Active / Inactive / TriggerOnce / Condition type.
- Add inline validation errors (target value semantics differ by condition).

### `/notifications` — Notifications

File: `InventoryAlert.UI/src/app/notifications/page.tsx`

Fetches:

- `GET /api/v1/notifications` (API returns paged result; UI uses `data.items` but does not implement paging controls)

Actions:

- Mark read:
  - `PATCH /api/v1/notifications/{id}/read`
- Mark all read:
  - `PATCH /api/v1/notifications/read-all`
- Dismiss:
  - `DELETE /api/v1/notifications/{id}`

Enhancement ideas:

- Add paging controls or infinite scroll using `page` + `pageSize` query params.
- Add filters: only unread, by symbol, by date range.
- Improve badge consistency: avoid decrementing unread count below 0 (currently guarded) and ensure unread count matches server state on refresh.

### `/admin/health` — System Integrity

File: `InventoryAlert.UI/src/app/admin/health/page.tsx`

Fetches:

- `GET /health` on interval (30s)

Enhancement ideas:

- Add explicit error UI (right now it only logs to console).
- Provide link-out buttons to Seq and Hangfire dashboards (if running locally).
- Make polling interval configurable (or pause when tab hidden).

### `/login` and `/register`

Files:

- `InventoryAlert.UI/src/app/(auth)/login/page.tsx`
- `InventoryAlert.UI/src/app/(auth)/register/page.tsx`

Notes:

- Login forces `window.location.href = '/'` after storing token, to sync all components.
- Register uses router push to `/login?registered=true`.

Enhancement ideas:

- Standardize visual design between login and register (styles differ significantly).
- Add password requirement hints aligned with API validation.

---

## 3) Components Inventory

All components are under `InventoryAlert.UI/src/components/*`.

### Global navigation + search

- `Navbar.tsx` — navigation links, sticky header, mobile menu
- `NavbarWrapper.tsx` — holds global search open state + keyboard shortcut Ctrl/Cmd+K
- `UserNav.tsx` — theme toggle, login/logout, notification bell, user avatar placeholder
- `GlobalSearch.tsx` — modal search for symbols; keyboard nav; uses `/api/v1/stocks/search`

Enhancement ideas:

- Add focus trap in `GlobalSearch` (escape closes, but focus management can be improved).
- Standardize z-index tokens (`GlobalSearch` uses `z-[200]`, other modals vary).

### Notifications (client state)

- `NotificationProvider.tsx` — SignalR connect + unread count bootstrap
- `NotificationBell.tsx` — shows unread badge; links to `/notifications`

Enhancement ideas:

- Reduce token polling (`setInterval(..., 2000)`); prefer a single “auth store” source of truth.
- Gate `console.log` noise behind a debug flag.

### Modals + dialogs

- `AddSymbolModal.tsx`
- `PriceAlertModal.tsx`
- `TradeModal.tsx`
- `ConfirmDialog.tsx`

Enhancement ideas:

- Create a shared `ModalShell` component (backdrop, panel, title row, close button, z-index, escape-to-close, focus trap).
- Fix duplicated input block in `TradeModal.tsx` (quantity field appears twice when opening a new position).

### Feedback

- `Toast.tsx` — simple toast with auto-dismiss

Enhancement ideas:

- Centralize toast via a provider so pages don’t duplicate toast state wiring.

### Market status

- `MarketStatusBanner.tsx` — polls `/api/v1/market/status` every 60s and displays US market open/closed badge

Enhancement ideas:

- Reuse this in `Navbar` or `Market` page for consistent “market open/closed” display.

---

## 4) Cross-Cutting Recommendations (High Impact)

### 4.1 Paging standardization (core request)

Pages that should implement paging because the API already supports it:

- Notifications (`/notifications`)
- Portfolio positions (`/portfolio`) if position count can grow
- News streams (Market + Stock detail)

Suggested common pagination component:

- `Pagination` with `page`, `pageSize`, `totalPages`, `onPageChange`
- optionally `InfiniteScroll` mode for notifications/news

### 4.2 Standardize confirm + error UX

Currently:

- some pages use `ConfirmDialog` + `Toast`
- some use `alert()` / `confirm()`

Recommendation:

- never use native `alert/confirm`
- one consistent pattern:
  - optimistic UI update + Toast
  - destructive action → ConfirmDialog

### 4.3 Align UI error parsing with backend `ErrorResponse`

Backend uses:

- `ErrorResponse { errors: [{ code, message, property }], errorId }`

Recommendation:

- create a single `getErrorMessage(err)` helper that:
  - prefers `ErrorResponse.errors[0].message`
  - falls back to generic message
  - optionally surfaces `errorId` and `X-Correlation-Id` for support

### 4.4 Reduce repeated Tailwind blobs

Recommendation:

- Introduce a small “UI kit” layer:
  - `PageHeader`, `Card`, `Button`, `Input`, `EmptyState`, `ErrorState`, `Skeleton`
- Refactor pages to use these, improving consistency and speed of iteration.

### 4.5 Rerender and state hygiene

Recommendation:

- Avoid long-lived intervals that rerun while tab is hidden:
  - for health checks and token sync, pause when `document.hidden`
- Consider `AbortController` for page fetches to avoid setting state after unmount/navigation.
