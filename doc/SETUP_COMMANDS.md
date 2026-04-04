# 🛠️ Project Setup — Names & Commands

> Run all commands from inside `InventoryManagementSystem/`
> unless stated otherwise.

---

## 📁 Final Project Names

| Project | Type | Purpose |
| :--- | :--- | :--- |
| `InventoryAlert.Api` | ASP.NET Web API | ✅ Existing — extended with event endpoints |
| `InventoryAlert.Contracts` | Class Library | ✅ Done — shared event schemas |
| `InventoryAlert.Worker` | Worker Service | ✅ Done — Hangfire + SQS consumer |
| `InventoryAlert.Sample` | Console App | ✅ Done — sample event publisher |
| `InventoryAlert.Tests` | xUnit Test | ✅ Existing |

---

## ⚡ Step 1 — Create New Projects

```powershell
cd InventoryManagementSystem

# 1. Shared contracts (event schemas)
dotnet new classlib -n InventoryAlert.Contracts -o InventoryAlert.Contracts

# 2. Background worker (Hangfire + SQS)
dotnet new worker -n InventoryAlert.Worker -o InventoryAlert.Worker

# 3. Sample publisher CLI
dotnet new console -n InventoryAlert.Sample -o InventoryAlert.Sample
```

---

## ⚡ Step 2 — Add to Solution

```powershell
dotnet sln add InventoryAlert.Contracts/InventoryAlert.Contracts.csproj
dotnet sln add InventoryAlert.Worker/InventoryAlert.Worker.csproj
dotnet sln add InventoryAlert.Sample/InventoryAlert.Sample.csproj
```

---

## ⚡ Step 3 — Add Project References

### `InventoryAlert.Api` references `Contracts`
```powershell
dotnet add InventoryAlert.Api/InventoryAlert.Api.csproj reference InventoryAlert.Contracts/InventoryAlert.Contracts.csproj
```

### `InventoryAlert.Worker` references `Contracts`
```powershell
dotnet add InventoryAlert.Worker/InventoryAlert.Worker.csproj reference InventoryAlert.Contracts/InventoryAlert.Contracts.csproj
```

### `InventoryAlert.Sample` references `Contracts`
```powershell
dotnet add InventoryAlert.Sample/InventoryAlert.Sample.csproj reference InventoryAlert.Contracts/InventoryAlert.Contracts.csproj
```

---

## ⚡ Step 4 — Install NuGet Packages

### `InventoryAlert.Api` — Event publishing
```powershell
# AWS SNS publisher (talks to Moto at localhost:5000)
dotnet add InventoryAlert.Api package AWSSDK.SimpleNotificationService
dotnet add InventoryAlert.Api package AWSSDK.SQS
```

### `InventoryAlert.Worker` — Hangfire + SQS consumer + Redis + DB
```powershell
# Hangfire core
dotnet add InventoryAlert.Worker package Hangfire.AspNetCore
dotnet add InventoryAlert.Worker package Hangfire.PostgreSql

# SQS consumer
dotnet add InventoryAlert.Worker package AWSSDK.SQS

# Redis caching
dotnet add InventoryAlert.Worker package Microsoft.Extensions.Caching.StackExchangeRedis

# EF Core (share same DB)
dotnet add InventoryAlert.Worker package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add InventoryAlert.Worker package Microsoft.EntityFrameworkCore.Design

# HTTP client (for Finnhub calls)
dotnet add InventoryAlert.Worker package RestSharp
```

### `InventoryAlert.Worker` — DynamoDB (Future Enterprise Extension)

> **⚠️ Optional:** Only install when migrating from Postgres `EventLog` and Redis dedup
> to the polyglot DynamoDB strategy described in `EVENT_DRIVEN_PLAN.md`.

```powershell
# DynamoDB client + object persistence model
dotnet add InventoryAlert.Worker package AWSSDK.DynamoDBv2
```

### `InventoryAlert.Sample` — HTTP client to call the API
```powershell
dotnet add InventoryAlert.Sample package Microsoft.Extensions.Http
```

---

## ⚡ Step 5 — Verify Solution Builds

```powershell
dotnet build InventoryManagementSystem.sln
```

---

## ⚙️ Step 6 — Environment Variables

The Worker's `.NET` config keys live in `InventoryAlert.Worker/appsettings.Docker.json`:

```json
{
  "Database": { "DefaultConnection": "host=db;port=5432;..." },
  "Redis":    { "Connection": "redis:6379" },
  "Aws": {
    "EndpointUrl":  "http://moto:5000",
    "SnsTopicArn":  "arn:aws:sns:us-east-1:123456789012:inventory-events",
    "SqsQueueUrl":  "http://moto:5000/123456789012/event-queue"
  }
}
```

AWS SDK credential vars must stay in `docker-compose.yml` (SDK reads OS env directly):

```env
AWS_ACCESS_KEY_ID=test
AWS_SECRET_ACCESS_KEY=test
AWS_DEFAULT_REGION=us-east-1
```

> **Note:** Moto accepts any value for credentials. Use `test` / `test` locally.

Future Telegram vars (add only when implementing Phase F):

```env
TELEGRAM_BOT_TOKEN=your_bot_token_here
TELEGRAM_CHAT_ID=your_chat_id_here
```

---

## 🐳 Step 7 — Docker Compose (Final State)

`docker-compose.yml` is fully configured. Services and startup order:

```yaml
services:
  api:       # REST API   — depends on: db (healthy), moto-init (completed)
  worker:    # Hangfire   — depends on: db (healthy), redis, moto-init (completed)
  db:        # PostgreSQL 17-alpine  — healthcheck: pg_isready
  redis:     # Redis 7.2-alpine      — healthcheck: redis-cli ping
  moto:      # motoserver/moto:5.1.22 — Mock SNS/SQS, healthcheck: curl :5000
  moto-init: # amazon/aws-cli:2.3.4  — runs init-sqs.sh ONCE on startup
```

> **Enforced startup order:**
> `db` + `redis` + `moto` (healthchecks pass) → `moto-init` (exits 0) → `api` + `worker`

### Worker Dockerfile

Located at `InventoryAlert.Worker/Dockerfile`. Multi-stage build using
`dotnet/runtime:10.0` (not `aspnet`) — the worker has no HTTP listener.

---

## 🛠️ Step 8 — Moto Init Script

Automatically creates SNS/SQS resources on first Docker boot:

- **Script:** `SolutionFolder/moto-init/init-sqs.sh`
- **Runs via:** `moto-init` service in docker-compose (one-shot, exits when done)

| Resource | Type | Notes |
| :--- | :--- | :--- |
| `inventory-event-dlq` | SQS Standard | Dead Letter Queue (max 3 receives) |
| `event-queue` | SQS Standard | Main queue — DLQ redrive wired |
| `inventory-events` | SNS Topic | Auto-subscribed to `event-queue` |

> Script is **idempotent** — checks before creating, safe to re-run.

---

## ✅ Quick Checklist

```
[x] dotnet new classlib    → InventoryAlert.Contracts          ✅ Done
[x] dotnet new worker      → InventoryAlert.Worker             ✅ Done
[x] dotnet new console     → InventoryAlert.Sample             ✅ Done
[x] dotnet sln add         → 3 projects added to .sln          ✅ Done
[x] dotnet add reference   → Contracts in Api + Worker + Sample ✅ Done
[x] dotnet add package     → All NuGet packages installed      ✅ Done
[x] Worker Dockerfile      → InventoryAlert.Worker/Dockerfile  ✅ Done
[x] appsettings.Docker     → Worker Docker config              ✅ Done
[x] init-sqs.sh            → SNS/SQS resources defined         ✅ Done
[x] docker-compose.yml     → All services + moto-init          ✅ Done
[x] dotnet build           → Full solution compiles            ✅ Done
[ ] TELEGRAM vars          → Add when implementing Phase F
```
