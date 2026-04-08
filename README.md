# InventoryAlert — Modern Financial Inventory Ecosystem

InventoryAlert is a high-performance inventory management and stock monitoring system built with **.NET 10 (Clean Architecture)** and **Next.js 15**. It provides real-time stock price tracking, automated alert evaluation via AWS SQS, and a premium developer experience through a centralized Wiki and Scalar API documentation.

---

## 🏗 Architecture Overview

The system is designed with strict **Domain-Driven Design (DDD)** principles and distributed observer patterns:

- **Api (.NET 10)**: High-throughput REST interface with JWT security and Scalar documentation.
- **Worker (.NET 10)**: Background engine handling Hangfire schedulers and AWS SQS consumers for price alerts.
- **Contracts**: Shared library for entities, common exceptions, and bus events to ensure zero circular dependencies.
- **UI (Next.js 15)**: OLED-themed dashboard using Tailwind CSS v4 and Nextra for the system Wiki.
- **Infrastructure**: Powered by **PostgreSQL** (Relational), **Redis** (Cache/Hangfire), and **Seq** (Structured Logging).

---

## 🚀 Quick Start (Development)

### 1. Infrastructure (One-Click Docker)
Spin up the database, cache, AWS emulator (LocalStack), and logging server:
```powershell
# Navigate to solution folder
cd InventoryManagementSystem
docker compose up -d db redis moto moto-init seq
```

### 2. Backend (API & Worker)
Run the core services from the root folder:
```powershell
# Run API
dotnet run --project InventoryManagementSystem/InventoryAlert.Api

# Run Worker
dotnet run --project InventoryManagementSystem/InventoryAlert.Worker
```
- **API Health**: `http://localhost:5294/health`
- **Interactive API Docs (Scalar)**: `http://localhost:5294/scalar/v1`

### 3. Frontend (UI & Wiki)
```powershell
cd InventoryAlert.UI
npm install
npm run dev
```
- **Dashboard**: `http://localhost:3000`
- **Project Wiki**: `http://localhost:3000/docs`

---

## 📚 Documentation & Observability

### System Wiki
The built-in Wiki provides deep-dives into:
- **Execution Flows**: Authentication and Alert evaluation sequences.
- **Database Model**: ERD and persistence strategy.
- **AI Agent Guide**: Protocol for coding assistants (Transaction patterns, async rules).

### Monitoring (Seq)
All internal messages and errors are streamed to **Seq** for real-time observability.
- **URL**: `http://localhost:5341`
- **Tracing**: Every request includes a `CorrelationId` that propagates from the API through SNS/SQS to the Worker.

---

## 🛠 Tech Stack
- **Backend**: C# 12, .NET 10, EF Core, Hangfire, MediatR, FluentValidation.
- **Frontend**: React 19, Next.js 15 (App Router), Tailwind CSS v4, Lucide Icons, Nextra.
- **Observability**: Serilog, Seq, OpenTelemetry.
- **Cloud/Infra**: PostgreSQL, Redis, AWS SNS/SQS (LocalStack/Moto).

---

## 📜 Maintenance Flow
Changes affecting the core architecture should follow the **Boy Scout Rule for Docs**: If you update a domain entity or a business flow, always update the corresponding entry in the [Project Wiki](./InventoryAlert.UI/src/pages/docs).
