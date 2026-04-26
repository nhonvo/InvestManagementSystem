# Microservice Component Interactions

> How the individual project components communicate at runtime.

## Request–Response Flow (API)

```mermaid
sequenceDiagram
    participant UI as Next.js UI
    participant API as InventoryAlert.Api
    participant Cache as Redis
    participant DB as PostgreSQL
    participant Finnhub as Finnhub API

    UI->>API: GET /api/v1/stocks/{symbol}/quote (Bearer token)
    API->>API: Validate JWT, extract userId
    API->>Cache: GET quote:{symbol}
    alt Cache HIT (< 30s)
        Cache-->>API: Cached quote JSON
    else Cache MISS
        API->>Finnhub: GET /api/v1/quote?symbol={symbol}
        Finnhub-->>API: { c, d, dp, h, l, o, pc }
        API->>Cache: SET quote:{symbol} TTL=30s
    end
    API-->>UI: 200 OK StockQuoteResponse

    UI->>API: POST /api/v1/portfolio/positions
    API->>DB: SELECT StockListing WHERE TickerSymbol = ?
    alt Symbol not in catalog
        API->>Finnhub: GET /stock/profile2?symbol=?
        Finnhub-->>API: Company profile data
        API->>DB: INSERT StockListing
    end
    API->>DB: INSERT Trade (Buy) + ExecuteTransactionAsync
    API-->>UI: 201 Created PortfolioPositionResponse
```

---

## Background Worker Flow (Price Sync)

```mermaid
sequenceDiagram
    participant HF as Hangfire Scheduler
    participant Job as SyncPricesJob
    participant Cache as Redis
    participant Finnhub as Finnhub API
    participant DB as PostgreSQL
    participant SQS as Amazon SQS

    HF->>Job: Trigger (every 15 minutes)
    Job->>DB: SELECT DISTINCT TickerSymbols FROM StockListing

    loop For each symbol
        Job->>Cache: GET quote:{symbol}
        alt Cache MISS
            Job->>Finnhub: GET /quote?symbol={symbol}
            Job->>Cache: SET quote:{symbol} TTL=30s
        end
        Job->>DB: INSERT PriceHistory
        Job->>DB: SELECT active AlertRules WHERE TickerSymbol
        loop For each AlertRule
            alt Condition breached AND cooldown not active
                Job->>DB: INSERT Notification
                Job->>Cache: SET cooldown:alert:{symbol} TTL=24h
                Job->>SQS: Publish inventoryalert.pricing.price-drop.v1
            end
        end
    end
    Job->>DB: SaveChangesAsync
```

---

## SQS Message Flow

```mermaid
flowchart LR
    API["InventoryAlert.Api"] -->|Publish| SQS["SQS: inventory-events"]
    HF["Hangfire Scheduler"] -->|Trigger| Worker["InventoryAlert.Worker"]
    Worker -->|Publish| SQS
    SQS -->|Consume| Router["IntegrationMessageRouter"]
    Router -->|price-drop.v1| PriceHandler["PriceAlertHandler"]
    Router -->|stock-low.v1| HoldingsHandler["LowHoldingsHandler"]
    Router -->|news.headline.v1| NewsHandler["CompanyNewsHandler"]
    Router -->|Unknown| DefaultHandler["DefaultHandler: Log + ACK"]
    PriceHandler --> DB[("PostgreSQL")]
    NewsHandler --> DDB[("DynamoDB")]
```

---

## Worker Isolation

The `InventoryAlert.Worker` runs as a **separate Docker container**. It communicates with `InventoryAlert.Api` only via:

1. **Shared PostgreSQL** — reads `StockListing`, writes `PriceHistory`, `Notification`, etc.
2. **Shared Redis** — reads/writes cache, dedup keys, cooldown keys.
3. **SQS** — consumes events published by the API, publishes events for handlers.
4. **SignalR Backplane (Redis)** — pushes real-time notifications from Worker to API-connected clients.

There are **no direct HTTP calls** between the API and Worker containers.
