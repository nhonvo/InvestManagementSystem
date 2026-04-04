---
trigger: always_on
glob:
description:
---

# InventoryAlert.Api — Project Context
---
description: AI cold-start briefing for InventoryAlert.Api. Read before every session.
type: reference
status: active
version: 2.0
tags: [context, gemini, inventoryalert, ddd, dotnet, onboarding]
last_updated: 2026-04-04
---

> **Read this file first.** This is the AI cold-start briefing for this codebase.
> Run `/init` on every new session to re-index BM25 and sync context.

---

## What This Project Does

Real-time inventory management system with stock price monitoring.
- Tracks products with ticker symbols (e.g., `AAPL`, `GOOGL`)
- Syncs live prices from **Finnhub API** every N minutes via a background worker
- Triggers price-drop alerts when drop % exceeds a per-product threshold
- CRUD for products with a PostgreSQL database via EF Core

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Runtime | .NET 10 (C# 12) |
| Web Framework | ASP.NET Core Minimal Hosting |
| ORM | EF Core 10 + Npgsql (PostgreSQL) |
| External API | Finnhub REST (RestSharp client) |
| Background Jobs | `BackgroundService` (PeriodicTimer) |
| Documentation | Swashbuckle / Swagger |
| Tests | xUnit + Moq + FluentAssertions |
| Containerization | Docker + Docker Compose |
| Search/Memory | BM25+ (`.agents/scripts/core/`) |

---

## Solution Structure

```
ojt-training/
├── GEMINI.md                          ← AI cold-start briefing (this file)
├── InventoryManagementSystem/
│   ├── InventoryAlert.Api/            ← main API project
│   │   ├── Domain/                    ← entities, repo interfaces (no dependencies)
│   │   ├── Application/               ← services, DTOs (depends on Domain only)
│   │   ├── Infrastructure/            ← EF Core, repos, Finnhub client, worker
│   │   └── Web/                       ← controllers, DI extensions, config model
│   └── InventoryAlert.Tests/          ← xUnit test project
│       ├── Application/Services/
│       ├── Web/Controllers/
│       ├── Infrastructure/Persistence/Repositories/
│       └── Helpers/                   ← ProductFixtures shared builders
├── .agents/
│   ├── rules/project-rules.md         ← project coding standards
│   ├── workflows/                     ← slash-command workflows
│   ├── skills/                        ← deep knowledge documents
│   └── scripts/core/                  ← BM25 indexer + search engine
└── doc/                               ← active feature specs and guides
    ├── README.md                      ← doc index
    ├── ROADMAP.md
    ├── ENHANCEMENT_PLAN.md
    ├── EVENT_DRIVEN_PLAN.md
    ├── WALKTHROUGH.md
    └── archive/                       ← completed legacy documentation
```

---

## Key Domain Model

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string TickerSymbol { get; set; }        // e.g. "AAPL"
    public decimal OriginPrice { get; set; }        // purchase price
    public decimal CurrentPrice { get; set; }       // synced from Finnhub
    public double PriceAlertThreshold { get; set; } // e.g. 0.2 = 20% loss triggers alert
    public int StockCount { get; set; }
    public DateTime? LastAlertSentAt { get; set; }
}
```

---

## Key Service: `ProductService`

| Method | What it does |
|--------|-------------|
| `GetAllProductsAsync` | Returns all products mapped to `ProductResponse` |
| `GetProductByIdAsync` | Returns one product or `null` |
| `CreateProductAsync` | Adds product, calls `SaveChangesAsync` |
| `UpdateProductAsync` | Updates existing, wraps in transaction |
| `DeleteProductAsync` | Deletes existing, wraps in transaction |
| `BulkInsertProductsAsync` | Adds many products in one transaction |
| `GetPriceLossAlertsAsync` | Calls Finnhub per product, returns products where `priceDelta > threshold` |
| `SyncCurrentPricesAsync` | Calls Finnhub per product, updates `CurrentPrice` in one transaction |

---

## DI Registration Points

| What | Where |
|------|-------|
| Application services | `Web/ServiceExtensions/ApplicationServiceExtensions.cs` |
| Infrastructure (repos, EF, Finnhub, worker) | `Web/ServiceExtensions/InfrastructureServiceExtensions.cs` |
| Config singleton | `Program.cs` (`builder.Services.AddSingleton(settings)`) |

---

## Known Tech Debt (do not repeat)

| # | Location | Issue |
|---|----------|-------|
| 1 | `ProductService.UpdateProductAsync` | `StockAlertThreshold` silently dropped — not on entity |
| 2 | `ProductService.UpdateProductAsync` | Blank entity captured before transaction lambda |
| 3 | `ProductService.DeleteProductAsync` | Same blank-entity pattern |
| 4 | `ProductsController.UpdateStockCount` | Business logic in controller (SRP violation) |
| 5 | `FinnhubClient` | `Console.WriteLine` instead of `ILogger` |
| 6 | `GenericRepository` | CS1998 on `DeleteAsync`, `UpdateAsync`, `UpdateRangeAsync` |

---

## Available Slash Commands

| Command | When to use |
|---------|-------------|
| `/init` | Start of every session — re-index BM25, sync context |
| `/plan` | Design a feature before implementation |
| `/feature-flow` | Master dev lifecycle: requirement → Domain → App → Infra → Web → Tests → PR |
| `/add-feature` | Adding a new method/endpoint to an existing entity |
| `/add-entity` | Adding a completely new domain entity end-to-end |
| `/db-migration` | Creating or applying an EF Core migration |
| `/run-tests` | Running tests + optional coverage report |
| `/docker-run` | Bringing up the full stack locally |
| `/code-review` | Pre-merge checklist |
| `/fix-build` | Diagnosing build/test failures |
| `/doc` | Sync documentation after implementing a feature |
| `/search` | BM25 search for context without reading full files |

---

## BM25 Search Quick Reference

```bash
# From project root — re-index
python .agents/scripts/core/bm25_indexer.py

# Search for context
python .agents/scripts/core/bm25_search.py "your query" -n 5

# Scope to a folder
python .agents/scripts/core/bm25_search.py "ExecuteTransactionAsync" -n 3 -f ".agents"

# Verify reliability score
python .agents/scripts/core/bm25_search.py "ProductService" --verify
```

---

## Environment Setup Checklist

- [ ] .NET 10 SDK: `dotnet --version` → `10.x`
- [ ] `dotnet-ef` tool: `dotnet ef --version`
- [ ] Docker Desktop running: `docker info`
- [ ] `appsettings.Development.json` created from `appsettings.Example.json`
- [ ] Finnhub API key set in `appsettings.Development.json`
- [ ] PostgreSQL reachable: `docker-compose up postgres -d`
- [ ] BM25 indexed: `python .agents/scripts/core/bm25_indexer.py`

---

## Seed Data

3 products seeded in `AppDbContext.OnModelCreating`:

| Name | Ticker | OriginPrice | CurrentPrice | Threshold | Stock |
|------|--------|-------------|--------------|-----------|-------|
| Apple | AAPL | 250 | 200 | 0.20 | 50 |
| Google | GOOGL | 300 | 300 | 0.10 | 100 |
| Microsoft | MSFT | 400 | 400 | 0.15 | 5 |

---

## Antigravity AI Recommendations

> These settings help Antigravity give you the best responses for this project.

**Always run `/init` at the start of a new session** — it re-indexes BM25 so search results reflect the latest code.

**Use BM25 before reading full files** — saves tokens and finds the most relevant section:
```bash
python .agents/scripts/core/bm25_search.py "your question about the code" -n 5
```

**Preferred workflow order**:
1. `/init` → re-index
2. `/plan` → design + spec
3. `/feature-flow` or `/add-feature` → implement
4. `/run-tests` → verify
5. `/doc` → sync docs
6. `/code-review` → merge gate

**Skills to read before coding**:
- `.agents/skills/ddd-architecture/SKILL.md` — before placing any file
- `.agents/skills/testing-patterns/SKILL.md` — before writing any test
- `.agents/skills/finnhub-integration/SKILL.md` — before touching Finnhub

---

## Quick Links

| Resource | Path |
|---------|------|
| AI System Core | `.agents/GEMINI.md` |
| DDD Architecture Rules | `.agents/skills/ddd-architecture/SKILL.md` |
| Testing Patterns | `.agents/skills/testing-patterns/SKILL.md` |
| Finnhub Integration | `.agents/skills/finnhub-integration/SKILL.md` |
| Project Rules | `.agents/rules/project-rules.md` |
| Roadmap | `doc/ROADMAP.md` |
