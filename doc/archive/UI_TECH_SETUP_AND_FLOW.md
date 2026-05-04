---
description: UI tech review, setup, folder structure, rendering/rerender flow, and Docker approach (documentation only).
type: reference
status: draft
version: 1.1
tags: [ui, nextjs, react, tailwind, docker, setup, architecture, inventoryalert]
last_updated: 2026-04-26
---

# InventoryAlert.UI — Tech, Setup, Structure, and Rendering Flow

This is a **documentation-only** review of the `InventoryAlert.UI` project:

- current tech stack and scripts,
- file/folder structure,
- how pages render and re-render,
- layout/provider flow,
- Docker approach for running UI + Wiki (recommendations; not implemented here),
- UX/UI enhancement ideas (recommendations; not implemented here).

Repository root: `InventoryAlert.UI/`

Companion doc (deeper inventory + per-page recommendations):

- `doc/UI_COMPONENTS_AND_PAGES_AUDIT.md`

---

## 1) Tech Stack (Observed)

From `InventoryAlert.UI/package.json`:

- **Next.js** `15.1.4` (App Router enabled — `src/app/*`)
- **React** `19.0.0`
- **Tailwind CSS** `v4` (imported via CSS: `@import "tailwindcss";`)
- **SignalR client**: `@microsoft/signalr` `^10.0.0`
- **Testing**: `vitest` + Testing Library + JSDOM
- **TypeScript** `^5`
- `next.config.ts` sets:
  - `output: "standalone"` (good for Docker)
  - `eslint.ignoreDuringBuilds = true`
  - `typescript.ignoreBuildErrors = true`

Notes:

- Ignoring type/build errors can mask real runtime issues; consider switching these back on once the UI stabilizes.

---

## 2) Scripts / Commands

From `package.json`:

- `npm run dev` → `next dev`
- `npm run build` → `next build`
- `npm run start` → `next start`
- `npm run lint` → `eslint`
- `npm run test` → `vitest run`

---

## 3) Environment Variables

Used today:

- `NEXT_PUBLIC_API_URL`
  - consumed by:
    - `InventoryAlert.UI/src/lib/api.ts`
    - `InventoryAlert.UI/src/hooks/useSignalR.ts`
  - default fallback in code is `http://localhost:8080`

Recommended convention:

- Dev local: `NEXT_PUBLIC_API_URL=http://localhost:8080`
- Docker (frontend + backend on same docker network): `NEXT_PUBLIC_API_URL=http://api:8080` (or the backend service name)

---

## 4) Folder Structure (Current)

Top-level:

- `InventoryAlert.UI/src/app/` — App Router routes (pages + layouts)
- `InventoryAlert.UI/src/components/` — UI components and providers
- `InventoryAlert.UI/src/hooks/` — custom hooks
- `InventoryAlert.UI/src/lib/` — API client + utilities
- `InventoryAlert.UI/public/` — static assets
- `InventoryAlert.UI/Dockerfile` — production image build (standalone output)

Routes (observed):

- `src/app/page.tsx` (Dashboard / Overview)
- `src/app/market/page.tsx`
- `src/app/stocks/page.tsx`
- `src/app/stocks/[symbol]/page.tsx`
- `src/app/portfolio/page.tsx`
- `src/app/watchlist/page.tsx`
- `src/app/alerts/page.tsx`
- `src/app/notifications/page.tsx`
- `src/app/admin/health/page.tsx`
- Auth:
  - `src/app/(auth)/login/page.tsx`
  - `src/app/(auth)/register/page.tsx`

Notes:

- `src/pages/` exists but appears empty. Consider removing it to reduce confusion (App Router-only project).

---

## 5) Layout + Provider Flow (How the App Boots)

Entry layout:

- `InventoryAlert.UI/src/app/layout.tsx`

Key behaviors:

- Injects an inline script to apply theme class (`dark`/`light`) early to reduce theme flash.
- Wraps the app with:
  - `ThemeProvider`
  - `NotificationProvider`
  - `NavbarWrapper` (owns global search open-state)
- Renders `children` inside a centered `<main>` container.

---

## 6) Rendering / Re-render Flow (What Triggers UI Updates)

### 6.1 Current pattern (client-first pages)

Many pages are declared as:

- `'use client'`
- Fetch on mount using `useEffect(() => load(), [])`
- Store results in component state via `useState`

Implications:

- Initial HTML is minimal; most data appears after client-side fetch completes.
- Re-render triggers are primarily:
  - state changes (`setState`)
  - dependency changes in effects (e.g., `newsCategory`, `newsPage`)

### 6.2 Example flows

Dashboard (`src/app/page.tsx`):

- On mount, calls:
  - `GET /api/v1/watchlist`
  - `GET /api/v1/market/news?...`
  - `GET /api/v1/portfolio/positions`
- Computes portfolio summary client-side.
- Enqueues news sync via:
  - `POST /api/v1/events` with `eventType = inventoryalert.news.sync-requested.v1`

Market (`src/app/market/page.tsx`):

- Re-fetches when `newsCategory` or `newsPage` changes.

Stocks (`src/app/stocks/page.tsx`):

- Switches between browse mode (`/stocks?page=`) vs search mode (`/stocks/search?q=`) based on input.

### 6.3 Notifications (SignalR live updates)

`InventoryAlert.UI/src/components/NotificationProvider.tsx`:

- Reads `auth_token` from `localStorage`.
- If token exists, connects to SignalR:
  - hub path: `/hubs/notifications` (full URL uses `NEXT_PUBLIC_API_URL`)
- On `ReceiveNotification`, increments unread count and prepends notification to local state.

Operational notes:

- This provider currently polls localStorage periodically (interval) to detect token changes; this can cause extra work/rerenders.
- Consider switching to an explicit “auth changed” event pattern in the future.

---

## 7) API Client (Current Behavior + Standardization Notes)

`InventoryAlert.UI/src/lib/api.ts`:

- Attaches `Authorization: Bearer <auth_token>` from localStorage when present.
- Always sends cookies (`credentials: "include"`) to support refresh token flow.
- On `401`:
  - tries refresh via `POST /api/v1/auth/refresh`
  - retries the original request once
  - if refresh fails, clears token and redirects to `/login`

Standardization suggestions (ties to API error docs):

- Parse `ErrorResponse` from the API consistently:
  - current backend error body uses `errors[]` + `errorId`
  - UI currently tries `userFriendlyMessage/message/title`, which may not match API response shape
- Prefer surfacing `X-Correlation-Id` to the UI for support/debug:
  - show it in error toasts, or expose a “Copy correlation id” affordance

See also:

- `doc/error_handling_and_response_standard.md`

---

## 8) Docker (UI + Wiki) — Recommended Approach (Not Implemented Here)

### 8.1 UI Docker (already exists)

`InventoryAlert.UI/Dockerfile` uses `next build` with `output: standalone` and runs `node server.js`.

Basic build/run:

```bash
docker build -t inventoryalert-ui ./InventoryAlert.UI
docker run --rm -p 3000:3000 -e NEXT_PUBLIC_API_URL=http://localhost:8080 inventoryalert-ui
```

### 8.2 Wiki Docker (recommended to add)

`InventoryAlert.Wiki/` does not currently include a Dockerfile.

Recommended pattern:

- build stage: `npm ci` + `npm run build`
- runtime stage: serve static `build/` output (nginx or a minimal node static server)

### 8.3 Two-container compose (recommended)

Recommended to create a frontend compose file (example name):

- `docker-compose.frontend.yml`
  - `ui` → `InventoryAlert.UI` on port `3000`
  - `wiki` → `InventoryAlert.Wiki` on port `3001`

This file is not added in this doc-only task.

---

## 9) UX/UI Enhancement Ideas (Recommended, Not Implemented)

High-impact improvements that also reduce duplicate code:

### 9.1 Introduce a small “UI kit” layer

Create reusable components:

- `PageHeader` (title, subtitle, actions)
- `Card`, `StatCard`
- `Table` / `TableSkeleton`
- `EmptyState`, `ErrorState`
- `Button`, `Input`, `Badge`

Then refactor pages to use them to reduce repeated Tailwind strings.

### 9.2 Centralize toast/confirm patterns

Currently many pages implement their own toast + confirm dialog state.

Recommended:

- a single global Toast provider + hook (`useToast()`)
- consistent error formatting (and optionally correlation id display)

### 9.3 Improve accessibility and keyboard UX

- Ensure modals trap focus, close on `Esc`, and are labeled correctly.
- Add `aria-live` for toast messages.

### 9.4 Reduce debug noise in production

- Gate `console.log` behind `NODE_ENV !== 'production'` or a `NEXT_PUBLIC_DEBUG_LOGS=true` flag.

---

## 10) Component / Hook / Page Scan (Observed)

This section is a “what exists today” inventory (paths are workspace-relative).

### 10.1 Pages (routes)

App Router pages:

- `InventoryAlert.UI/src/app/page.tsx` → `/`
- `InventoryAlert.UI/src/app/market/page.tsx` → `/market`
- `InventoryAlert.UI/src/app/stocks/page.tsx` → `/stocks`
- `InventoryAlert.UI/src/app/stocks/[symbol]/page.tsx` → `/stocks/:symbol`
- `InventoryAlert.UI/src/app/portfolio/page.tsx` → `/portfolio`
- `InventoryAlert.UI/src/app/watchlist/page.tsx` → `/watchlist`
- `InventoryAlert.UI/src/app/alerts/page.tsx` → `/alerts`
- `InventoryAlert.UI/src/app/notifications/page.tsx` → `/notifications`
- `InventoryAlert.UI/src/app/admin/health/page.tsx` → `/admin/health`

Auth group pages:

- `InventoryAlert.UI/src/app/(auth)/login/page.tsx` → `/login`
- `InventoryAlert.UI/src/app/(auth)/register/page.tsx` → `/register`

Global layout:

- `InventoryAlert.UI/src/app/layout.tsx`

Static assets / styles:

- `InventoryAlert.UI/src/app/globals.css`
- `InventoryAlert.UI/src/app/favicon.ico`

### 10.2 Components

`InventoryAlert.UI/src/components/*`:

- `AddSymbolModal.tsx`
- `ConfirmDialog.tsx`
- `GlobalSearch.tsx`
- `MarketStatusBanner.tsx` (+ `MarketStatusBanner.test.tsx`)
- `Navbar.tsx`
- `NavbarWrapper.tsx`
- `NotificationBell.tsx` (+ `NotificationBell.test.tsx`)
- `NotificationProvider.tsx`
- `PriceAlertModal.tsx`
- `ThemeProvider.tsx`
- `Toast.tsx`
- `TradeModal.tsx`
- `UserNav.tsx`

### 10.3 Hooks

`InventoryAlert.UI/src/hooks/*`:

- `useSignalR.ts`

### 10.4 Client utilities

`InventoryAlert.UI/src/lib/*`:

- `api.ts` (+ `api.test.ts`, `api.refresh.test.ts`)

Notes:

- `InventoryAlert.UI/src/pages/` exists but appears empty (App Router-only project).

---

## 11) Paging + Consistency Targets (Recommended, Not Implemented)

Where paging already exists in state but UX is incomplete:

- `InventoryAlert.UI/src/app/market/page.tsx` — `newsPage` exists; add Next/Prev controls and show current page.
- `InventoryAlert.UI/src/app/stocks/[symbol]/page.tsx` — News tab has `newsPage`; add visible paging controls.

Where API is paged but UI does not expose paging:

- `InventoryAlert.UI/src/app/notifications/page.tsx` — API returns paged result; add paging UI or infinite scroll.
- `InventoryAlert.UI/src/app/portfolio/page.tsx` — response is `PagedResult`; add paging controls if position count can grow.

Recommended reusable primitives:

- `Pagination` (page + pageSize + totalPages)
- `EmptyState` / `ErrorState` (standard handling)
- Replace all `alert()` / `confirm()` with `Toast` + `ConfirmDialog`

See the per-page details in `doc/UI_COMPONENTS_AND_PAGES_AUDIT.md`.

---

## 12) Next Documentation Targets (If You Want)

If you want, I can create additional docs (still markdown-only) for:

- **UI API contract mapping** (endpoint list + response DTOs used by UI)
- **UI auth flow** (login/refresh/logout lifecycle)
- **UI notification flow** (SignalR events + unread count semantics)
