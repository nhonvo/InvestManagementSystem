# Project Wiki Implementation Plan

This plan outlines the structure and content for a centralized Wiki/Documentation repository designed to onboarding new developers, provide clarity for users, and optimize context for AI coding agents.

## 1. Goal
Create a single source of truth that defines the technical and functional landscape of the **InventoryAlert** ecosystem.

---

## 2. Wiki Structure (The "Knowledge Map")

### 📂 01. Tech Stack & Environment
- **Backend**: .NET 10, C# 12, Entity Framework Core 10, Npgsql.
- **Frontend**: Next.js 15, TypeScript, Tailwind CSS (v4), React Server Components.
- **Infrastructure**: PostgreSQL, Seq (Logging), Hangfire (Scheduler), SNS/SQS (Event Bus), Docker.
- **Tools**: VS Code/Visual Studio, BM25 Search (Agent), GitHub Actions.

### 🏛️ 02. Architecture (The "Blueprints")
- **Conceptual Model**: Domain-Driven Design (DDD) + Clean Architecture.
- **Layering**:
    - **Web (Api)**: Controllers, Middleware, Auth.
    - **Application**: Use Cases, Services, DTOs, Mappings.
    - **Domain**: Entities, Interfaces, Exceptions, Constants.
    - **Infrastructure**: Repositories, DB Context, External Clients (Finnhub).
    - **Worker**: Background processors, Event consumers.

### 💾 03. Data Model (The "Schema")
- **Core Entities**: `User`, `Product`, `WatchlistItem`, `AlertRule`, `PriceHistory`.
- **Database**: PostgreSQL schema definitions and relationship maps.
- **Worker Data**: DynamoDB/Local cache entities for price/news tracking.

### 🔄 04. Execution Flows (The "Sequences")
- **Authentication**: JWT issues, Token Storage, Protected Routes.
- **Alert Lifecycle**: 
    1. Rule defined in UI.
    2. Event triggered (Manual or Timer).
    3. Worker processes Price/News.
    4. Evaluates rule conditions.
    5. Dispatches alert (Telegram/Log).
- **Price Sync**: Scheduled tasks vs. Manual triggers.

### ⚙️ 05. Background Jobs (The "Engine")
- **Hangfire Dashboard**: How to monitor internal scheduled tasks.
- **Scheduled Workers**: `FinnhubPricesSyncWorker`, `MarketStatusWorker`.
- **Queue System**: SQS message processing patterns.

### 🛠️ 06. Developer Guide (The "Onboarding")
- **Local Setup**: `docker compose up`, `dotnet run`, `npm dev`.
- **Coding Standards**: SOLID, Primary Constructors, Async/Await rules.
- **Testing**: xUnit, Moq, Integration test patterns.

### 🤖 07. AI Agent Optimization (The "Context")
- **Knowledge items (KIs)**: Repository-specific patterns (e.g., Transaction Capture Pattern).
- **Workflows**: `feature-flow`, `add-entity`, `db-migration` instructions.
- **Corpus Mapping**: How to search effectively using BM25.

---

## 3. Implementation Phases

| Phase | Content focus | Output |
| :--- | :--- | :--- |
| **Phase 1** | **Foundation** | Architecture, Tech Stack, Local Setup. |
| **Phase 2** | **Technical Core** | Data Model, Controllers, Repositories. |
| **Phase 3** | **Dynamic Flows** | Alert Flow, Worker Jobs, Event Bus. |
| **Phase 4** | **AI Layer** | Knowledge Items, Workflow documentation. |

---

## 4. Maintenance Strategy
- **Doc-as-Code**: Keep all markdown files in `docs/wiki/` within the main repository.
- **Sync Routine**: Update the wiki as part of the `feature-flow` cleanup.
