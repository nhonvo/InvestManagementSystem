---
slug: /
sidebar_position: 1
---

# 🚀 InventoryAlert Documentation

Welcome to the **InventoryAlert** System Wiki! This documentation hub serves as the single source of truth for both the architectural blueprints and the operational runbooks of the project.

InventoryAlert is a real-time stock monitoring ecosystem built on **.NET 10** and **Next.js 15**. It tracks stock prices automatically, evaluates user-defined Alert Rules, and dispatches Telegram notifications in real-time when thresholds are crossed.

---

## 🧭 Navigating the Wiki

All documentation is organized into chronological, logical sections found in the sidebar. Click into any section to start exploring:

### 🌟 Project & Architecture
Understand the "why" and the overall shape of the system.
*   **[Project Introduction](./00-overview/introduction.md)** — Core features and business value.
*   **[Architecture & Tech Stack](./02-architecture-techstack/architecture-overview.md)** — C4 Context diagrams, software stacks, and interaction maps.
*   **[Domain-Driven Design](./02-architecture-techstack/domain-and-structure.md)** — Explains the strict Clean Architecture layers used in the .NET backend.

### 💾 Core Logic & Flows
Deep dive into the data rules and sequence flows.
*   **[Data Model](./03-data-model/data-model.md)** — Complete PostgreSQL Entity-Relationship diagrams and Alert Rule trigger logic.
*   **[Execution Flows](./04-execution-flows/user-authentication.md)** — Step-by-step sequence diagrams showing exactly how data moves across the system.

### 🔌 APIs & Jobs
Explore how the system talks to the outside world and manages background workloads.
*   **[API Services](./05-api-services/internal-api.md)** — Reference for the internal REST controllers and external Finnhub/Telegram integrations.
*   **[Background Workers](./06-background-jobs/workers.md)** — A deep dive into the Hangfire job scheduler, Amazon SQS polling, and Finnhub Prices worker.

### 🛠️ Developer Guide
Everything you need to run, build, and maintain the platform.
*   **[Getting Started & Local Deploy](./07-dev-maintenance/getting-started.md)** — `docker compose` commands and the ultimate dev cheat sheet.
*   **[Testing & Standards](./07-dev-maintenance/testing-and-standards.md)** — xUnit Moq patterns and C# 12 coding rules.
*   **[Operational Runbook](./07-dev-maintenance/operational-runbook.md)** — Standard operating procedures for checking DBs, reading Seq logs, and debugging the Docker stack.

### 🖥️ Front-End
*   **[Frontend UI Application](./08-frontend-ui/ui-architecture.md)** — An intensive breakdown of the Next.js App Router layout, Server Components vs Client hooks, and the alert rule UI logic.

---

> **Tip!** Use the Search bar in the top right to instantly jump to specific API paths, variables, or architecture diagrams anywhere in the Wiki!
