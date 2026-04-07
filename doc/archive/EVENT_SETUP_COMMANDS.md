# 🛠️ Project Setup — Names & Commands

> Run all commands from inside `InventoryManagementSystem/`
> unless stated otherwise.

---

## 📁 Final Project Names

| Project                    | Type            | Purpose                                         |
| :------------------------- | :-------------- | :---------------------------------------------- |
| `InventoryAlert.Api`       | ASP.NET Web API | ✅ Existing — extended with event endpoints      |
| `InventoryAlert.Contracts` | Class Library   | ✅ Done — shared event schemas + domain entities |
| `InventoryAlert.Worker`    | Worker Service  | ✅ Done — Hangfire + SQS consumer               |
| `InventoryAlert.Sample`    | Console App     | ✅ Done — sample event publisher                |
| `InventoryAlert.Tests`     | xUnit Test      | ✅ Existing                                     |

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

## ⚙️ Step 6 — Environment Variables Needed

Add these to your `.env` file (and `.env.example`):

```env
# Existing
DB_PASSWORD=password
FINNHUB_API_KEY=your_key_here

# New — AWS / Moto
AWS_ACCESS_KEY_ID=test
AWS_SECRET_ACCESS_KEY=test
AWS_DEFAULT_REGION=us-east-1
AWS_ENDPOINT_URL=http://moto:5000        # inside Docker
# AWS_ENDPOINT_URL=http://localhost:5000 # from host machine

# New — SNS/SQS resource names (Moto will create these on startup)
SNS_TOPIC_ARN=arn:aws:sns:us-east-1:123456789012:inventory-events
SQS_QUEUE_URL=http://moto:5000/123456789012/event-queue

# New — Redis
REDIS_CONNECTION=redis:6379              # inside Docker
# REDIS_CONNECTION=localhost:6379        # from host machine

# Future — Telegram
TELEGRAM_BOT_TOKEN=your_bot_token_here
TELEGRAM_CHAT_ID=your_chat_id_here
```

> **Note:** Moto accepts any value for `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY`.
> Use `test` / `test` locally — it works without real credentials.

---

## 🐳 Step 7 — Add Worker to `docker-compose.yml`

```yaml
worker:
  build:
    context: .
    dockerfile: InventoryAlert.Worker/Dockerfile
  container_name: inventory_worker
  environment:
    - ASPNETCORE_ENVIRONMENT=Docker
    - Database__DefaultConnection=host=db;port=5432;Database=InventoryAlertDb;Username=postgres;Password=password
    - Redis__Connection=redis:6379
    - Aws__EndpointUrl=http://moto:5000
    - Aws__SnsTopicArn=arn:aws:sns:us-east-1:123456789012:inventory-events
    - Aws__SqsQueueUrl=http://moto:5000/123456789012/event-queue
  depends_on:
    db:
      condition: service_healthy
  networks:
    - app-network
  restart: unless-stopped
```

---

## ✅ Quick Checklist

```
[✅] dotnet new classlib  → InventoryAlert.Contracts      ✅ Done
[✅] dotnet new worker    → InventoryAlert.Worker         ✅ Done
[✅] dotnet new console   → InventoryAlert.Sample         ✅ Done
[✅] dotnet sln add       → 3 projects added to .sln      ✅ Done
[✅] dotnet add reference → Contracts in Api + Worker + Sample ✅ Done
[✅] dotnet add package   → All NuGet packages installed  ✅ Done
    - Api: AWSSDK.SNS + SQS + Serilog + FluentValidation
    - Worker: Hangfire + SQS + Redis + EF + RestSharp
    - Sample: AWSSDK.SNS + SQS + Microsoft.Extensions.Http
[✅] Entities migrated    → Contracts/Entities/ folder    ✅ Done
    (Product, EventLog, EarningsRecord, InsiderTransaction, NewsRecord)
[✅] .env                 → AWS + Redis keys added        ✅ Done
[✅] docker-compose.yml   → Worker service added          ✅ Done
[✅] dotnet build         → Full solution compiles        ✅ Done
[✅] dotnet test          → 61/61 tests pass              ✅ Done
```
