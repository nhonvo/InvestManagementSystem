# Architecture Overview

## System Context

InventoryAlert is a real-time stock alerting system. The diagram below shows how it interacts with external systems.

```mermaid
graph TD
    User["🧑 Portfolio User"] -->|HTTPS| UI["Next.js UI :3000"]
    UI -->|REST/HTTPS| API["InventoryAlert.Api :8080"]
    API --> PG[("PostgreSQL")]
    API --> SQS["Amazon SQS"]
    Worker["InventoryAlert.Worker"] --> PG
    Worker -->|REST| Finnhub["Finnhub API"]
    Worker -->|REST| Telegram["Telegram Bot"]
    Worker --> SQS
    API --> Seq["Seq Logger :5341"]
    Worker --> Seq
```

## Tech Stack

### Backend
| Layer | Technology |
|---|---|
| Runtime | .NET 10 / C# 12 |
| Web Framework | ASP.NET Core 10 |
| ORM | Entity Framework Core 10 + Npgsql |
| Background Jobs | Hangfire (PostgreSQL storage) |
| Event Bus | Amazon SQS / SNS |
| Logging | Serilog → Seq |

### Frontend
| Layer | Technology |
|---|---|
| Framework | Next.js 15 (App Router + RSC) |
| Language | TypeScript |
| Styling | Tailwind CSS v4 |

### Infrastructure
| Component | Tool |
|---|---|
| Database | PostgreSQL 17 |
| Observability | Seq (structured logs) |
| Containerization | Docker + Docker Compose |
| Cloud Events | Amazon SQS / SNS |
| Notifications | Telegram Bot API |
