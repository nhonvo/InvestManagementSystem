---
slug: /
sidebar_position: 1
---

# 🚀 InventoryAlert Documentation (v2)

Welcome to the **InventoryAlert** System Wiki — the single source of truth for architecture, operational runbooks, and API contracts.

InventoryAlert is a real-time stock portfolio and alerting ecosystem built on **.NET 10** and **Next.js 15**. It tracks global stock prices via Finnhub, evaluates user-defined Alert Rules, and delivers in-app notifications when thresholds are crossed.

> **v2 Note**: The system has been fully refactored from an inventory-management domain to a finance-domain vocabulary. `Product` → `StockListing`, `StockTransaction` → `Trade`, Telegram → In-App Notifications.

---

## 🧭 Navigating the Wiki

### 🌟 Project & Architecture

Understand the "why" and the overall shape of the system.

- **[Project Introduction](./00-overview/introduction.md)** — Core features, capabilities, and seed accounts.
- **[Architecture Overview](./02-architecture-techstack/architecture-overview.md)** — System diagram, tech stack, Docker service map.
- **[Domain & Structure](./02-architecture-techstack/domain-and-structure.md)** — Clean Architecture layers, solution folder layout, placement rules.
- **[Microservice Components](./02-architecture-techstack/microservice-components.md)** — Runtime interaction between API, Worker, Redis, SQS, Finnhub.

### 💾 Core Logic & Flows

Deep dive into the data rules and execution sequences.

- **[Data Model](./03-data-model/data-model.md)** — PostgreSQL ER diagram, DynamoDB tables, AlertCondition enum.
- **[Caching Strategy](./03-data-model/caching-strategy.md)** — Redis key namespaces, TTLs, and rate limit guard.
- **[User Authentication](./04-execution-flows/user-authentication.md)** — JWT lifecycle, refresh rotation, claim structure.
- **[Price Sync Flow](./04-execution-flows/price-sync-flow.md)** — End-to-end price fetch → PriceHistory → alert evaluation → notification.
- **[Alert Dispatch](./04-execution-flows/alert-dispatch.md)** — AlertRule evaluation, Redis cooldown, SQS relay.
- **[Business Logic](./04-execution-flows/business-logic.md)** — Trade ledger semantics, symbol discovery, cascade delete rules, validation.

### 🔌 APIs & Jobs

How the system talks to the outside world and background workloads.

- **[Internal API Reference](./05-api-services/internal-api.md)** — All v2 endpoints: Auth, Portfolio, Stocks, Market, Watchlist, AlertRules, Notifications, Events.
- **[Event Handling](./05-api-services/event-handling.md)** — SQS topology, event type strings, deduplication pattern.
- **[External Integrations](./05-api-services/external-integrations.md)** — Finnhub, SQS, DynamoDB, Redis integration details.
- **[Background Workers](./06-background-jobs/workers.md)** — All 10 Hangfire jobs with cron schedules and Finnhub endpoints.
- **[Hangfire Monitoring](./06-background-jobs/hangfire-monitoring.md)** — Dashboard guide, job catalog, failure impact.

### 🛠️ Developer Guide

Everything you need to run, build, and maintain the platform.

- **[Getting Started](./07-dev-maintenance/getting-started.md)** — Docker commands, seed credentials, service URLs.
- **[.NET CLI Reference](./07-dev-maintenance/dotnet-commands.md)** — Migrations, test commands, publish guide.
- **[Testing & Standards](./07-dev-maintenance/testing-and-standards.md)** — xUnit patterns, C# 12 rules, E2E base setup.
- **[Operational Runbook](./07-dev-maintenance/operational-runbook.md)** — Troubleshooting guide, DB queries, common debug scenarios.
- **[AI Agent Workflows](./07-dev-maintenance/ai-agent-workflows.md)** — Slash commands, transaction capture patterns, file placement rules.

### 🖥️ Front-End

- **[UI Architecture](./08-frontend-ui/ui-architecture.md)** — App Router layout, page → API map, component library, state management.
- **[Feature Breakdown](./08-frontend-ui/feature-breakdown.md)** — All user-facing pages, notification hub flow, symbol discovery UX.

---

> 💡 **Tip**: Use the sidebar search to instantly jump to specific API paths, entity names, or architecture diagrams.
