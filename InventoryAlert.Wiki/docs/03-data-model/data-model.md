# Data Model

## Entity Relationship Diagram

```mermaid
erDiagram
    User ||--o{ WatchlistItem : watches
    User ||--o{ AlertRule : configures
    User ||--o{ Trade : executes

    StockListing ||--o{ PriceHistory : records
    StockListing ||--o{ WatchlistItem : "tracked via Symbol"
    StockListing ||--o{ Trade : "referenced via Symbol"
    StockListing ||--o| StockMetric : "has cached metrics"
    StockListing ||--o{ EarningsSurprise : has
    StockListing ||--o{ RecommendationTrend : has
    StockListing ||--o{ InsiderTransaction : has

    User {
        Guid Id PK
        string Username UK
        string PasswordHash
        string Email
        string Role
        datetime CreatedAt
    }
    StockListing {
        int Id PK
        string Name
        string TickerSymbol UK
        string Exchange
        string Currency
        string Country
        string Industry
        decimal MarketCap
        date Ipo
        string WebUrl
        string Logo
        datetime LastProfileSync
    }
    WatchlistItem {
        Guid UserId PK, FK
        string TickerSymbol PK, FK
        datetime CreatedAt
    }
    AlertRule {
        Guid Id PK
        Guid UserId FK
        string TickerSymbol FK
        AlertCondition Condition
        decimal TargetValue
        bool IsActive
        bool TriggerOnce
        datetime LastTriggeredAt
        datetime CreatedAt
    }
    Trade {
        Guid Id PK
        Guid UserId FK
        string TickerSymbol FK
        TradeType Type
        decimal Quantity
        decimal UnitPrice
        datetime TradedAt
        string Notes
    }
    PriceHistory {
        long Id PK
        string TickerSymbol FK
        decimal Price
        decimal High
        decimal Low
        decimal Open
        decimal PrevClose
        datetime RecordedAt
    }
    StockMetric {
        string TickerSymbol PK, FK
        double PeRatio
        double PbRatio
        double EpsBasicTtm
        double DividendYield
        decimal Week52High
        decimal Week52Low
        double RevenueGrowthTtm
        double MarginNet
        datetime LastSyncedAt
    }
    EarningsSurprise {
        int Id PK
        string TickerSymbol FK
        date Period UK
        double ActualEps
        double EstimateEps
        double SurprisePercent
        date ReportDate
    }
    RecommendationTrend {
        int Id PK
        string TickerSymbol FK
        string Period
        int StrongBuy
        int Buy
        int Hold
        int Sell
        int StrongSell
        datetime SyncedAt
    }
    InsiderTransaction {
        int Id PK
        string TickerSymbol FK
        string Name
        long Share
        decimal Value
        date TransactionDate
        date FilingDate
        string TransactionCode
    }
    Notification {
        Guid Id PK
        Guid UserId FK
        Guid AlertRuleId FK
        string TickerSymbol FK
        string Message
        NotificationType Type
        NotificationSeverity Severity
        bool IsRead
        datetime CreatedAt
    }
```

---

## High-Performance Normalized Model

InventoryAlert uses a **Unified Global Architecture** to handle market-scale stock data while maintaining private user isolation.

### 1. Global Domain (PostgreSQL — `StockListings`, `PriceHistories`, `StockMetrics`, etc.)

Market reference data and analytical intelligence are stored once and shared across all users.

| Entity | Purpose |
|---|---|
| `StockListing` | Core market reference data (Ticker, Finnhub metadata) |
| `PriceHistory` | Point-in-time price snapshots for rendering charts and evaluating rules |
| `StockMetric` | Cached basic financials (PE, PB, margins, 52-week highs) |
| `EarningsSurprise` | Last 4 quarters of earnings actuals vs. estimates (Finnhub) |
| `RecommendationTrend` | Analyst consensus ratings |
| `InsiderTransaction` | Last 100 insider SEC filings |

> **Key Design Decision**: Symbol Resolution pattern mandates a DB-First check, falling back to Finnhub `/profile2` API on cache misses, permanently persisting new symbols into `StockListing`.

### 2. User Domain (PostgreSQL — `Trades`, `AlertRules`, `WatchlistItems`, `Notifications`)

Stored per-user. Fully isolated.

| Entity | User-Specific Data |
|---|---|
| `Trade` | Ownership ledger (Buy/Sell). Replaces older stock-counting structures to allow dynamic cost-basis tracking. |
| `AlertRule` | Supports specific target values, conditions (e.g. `PriceAbove`, `PercentDropFromCost`), and one-off/recurring modes. |
| `WatchlistItem` | Minimal join table linking a User to a TickerSymbol. |
| `Notification` | System and alert messages securely scoped to the user, synced with a UI bell-badge feed. |

### 3. Historical Archives (Amazon DynamoDB — `CompanyNews`, `MarketNews`)

Used for high-volume, unstructured market data that is never deleted.

| Table | PK | SK | Responsibility |
|---|---|---|---|
| `MarketNews` | `CATEGORY#<category>` | `TS#<unix_timestamp>` | General financial events covering forex/crypto/markets |
| `CompanyNews` | `SYMBOL#<ticker>` | `TS#<unix_timestamp>` | Ticker-specific press releases and articles |

---

## AlertCondition Enum

The robust trigger model handles both absolute value checks and advanced cost-basis evaluation:

```csharp
public enum AlertCondition
{
    PriceAbove,              // CurrentPrice > TargetValue
    PriceBelow,              // CurrentPrice < TargetValue
    PriceTargetReached,      // CurrentPrice == TargetValue (within technical bounds)
    PercentDropFromCost,     // Evaluates loss% dynamically via SUM(CostBasis)
    LowHoldingsCount         // Trigger when user's share count drops below TargetValue
}
```

## Notification Enums

```csharp
public enum NotificationType
{
    Price,
    Holdings,
    News,
    System
}

public enum NotificationSeverity
{
    Info,
    Warning,
    Critical
}
```

---

## Alert Evaluation Logic (Fan-Out)

InventoryAlert **v3** uses a high-performance **Hybrid Pipeline**:

```mermaid
flowchart TD
    A["Trigger: Scheduled (15m) or Event (Trade/Price)"] --> B["Identify Ticker and User Context"]
    B --> C["Load Active Rules from PostgreSQL"]
    C --> D["Evaluate via Shared IAlertRuleEvaluator"]
    D --> E{Breach Identified?}
    E -- Yes --> F{Redis Cooldown active?}
    F -- No --> G["1. Insert Notification (w/ Type & Severity)"]
    G --> H["2. SET Redis Cooldown (Rule-specific)"]
    G --> I["3. SignalR Push (Instant UI Sync)"]
    H & I --> J{TriggerOnce?}
    J -- Yes --> K["Disable AlertRule"]
```

### Business Rules

| Rule | Detail |
|---|---|
| **TriggerOnce** | If `Rule.TriggerOnce` is true, the rule is automatically disabled (`IsActive = false`) after firing. |
| **Deduplication** | Managed via Redis keys: `alert:cooldown:{userId}:{ruleId}` to prevent notification spam. |
| **Real-time Delivery** | Every notification is pushed via **SignalR** using the **Redis Backplane**. |
| **Cascade Behavior** | Deleting a position deletes owned trades and alerts. **Does not** delete the global `StockListing`. |
| **Notification Feed** | Instead of just sending alerts, breaches generate `Notification` rows to populate the web UI hub. |
