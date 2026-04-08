# Frontend Architecture

> How the Next.js 15 UI application is structured, fetches data, and manages state.

## App Router Structure

```
InventoryAlert.UI/
├── src/
│   ├── app/
│   │   ├── (auth)/               ← Login / Register pages
│   │   ├── dashboard/            ← Main product dashboard
│   │   ├── watchlists/           ← Watchlist management
│   │   ├── alert-rules/          ← Alert rule CRUD
│   │   └── layout.tsx            ← Root layout with providers
│   ├── components/               ← Reusable UI components
│   ├── hooks/                    ← Custom React hooks
│   └── lib/                      ← API client, auth helpers
```

## Component Model & State

- **Server Components**: Used for data-fetching pages (dashboard, product list)
- **Client Components**: Used for interactive forms, real-time state (alert rule editor)

## Auth Flow in UI

The token is stored in an `HttpOnly` cookie. The middleware in `middleware.ts` protects all routes under `/(dashboard)`.

---

## Data Fetching

### Server-Side Data Fetching

Product lists and dashboards use React Server Components with Next.js `fetch`:

```typescript
// app/dashboard/page.tsx (Server Component)
const products = await fetch(`${process.env.API_URL}/products`, {
  headers: { Authorization: `Bearer ${token}` },
  next: { revalidate: 30 },  // ISR: refresh every 30s
});
```

### Client-Side Mutations

Alert rule creation uses a Client Component with `useState` + `fetch`:

```typescript
// components/AlertRuleForm.tsx
'use client';
const handleSubmit = async (data: AlertRuleDto) => {
  await fetch('/api/alert-rules', { method: 'POST', body: JSON.stringify(data) });
};
```

### API Route Proxy

To avoid CORS issues, the UI proxies API calls through Next.js route handlers in `app/api/`:

```
/app/api/alert-rules/route.ts  →  forwards to http://api:8080/alert-rules
```
