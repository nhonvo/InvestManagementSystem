# Portfolio Management Flow

> Orchestration for user-specific portfolios, trade history, and real-time performance tracking.

## Overview

The Portfolio flow manages the lifecycle of a user's financial positions. It tracks **Trades** (Buys/Sells), computes **Cost Basis**, and aggregates real-time **Market Value** and **Total Return** using live price data.

---

## Position Lifecycle (Sequence)

```mermaid
sequenceDiagram
    participant User as User / Frontend
    participant API as PortfolioController
    participant Service as PortfolioService
    participant DB as PostgreSQL
    participant SDS as StockDataService

    Note over User,SDS: Part 1: Opening a Position
    User->>API: POST /api/v1/portfolio/positions
    API->>Service: OpenPositionAsync(request, userId)
    Service->>DB: BEGIN Transaction
    Service->>DB: INSERT INTO WatchlistItems (Ensure tracked)
    Service->>DB: INSERT INTO Trades (Type=Buy, Qty, Price)
    Service->>DB: COMMIT Transaction
    
    Note over User,SDS: Part 2: Recording subsequent Trades
    User->>API: POST /api/v1/portfolio/{symbol}/trades
    API->>Service: RecordTradeAsync(symbol, tradeRequest, userId)
    Service->>DB: SELECT SUM(Qty) FROM Trades (Net Holdings)
    alt Type = Sell AND NetHoldings < request.Qty
        Service-->>API: Throw InsufficientHoldingsException
    end
    Service->>DB: INSERT INTO Trade
    Service->>DB: SaveChangesAsync()
    
    Note over User,SDS: Part 3: Performance Calculation
    Service->>SDS: GetQuoteAsync(symbol)
    Service->>Service: Compute AvgPrice, MarketValue, TotalReturn
    Service-->>API: PortfolioPositionResponse
    API-->>User: 201 Created / 200 OK
```

---

## Performance Calculation Logic

The system computes performance metrics on-the-fly to ensure accuracy:

1.  **Net Holdings**: `SUM(BuyQuantity) - SUM(SellQuantity)`
2.  **Total Buy Cost**: `SUM(BuyQuantity * BuyPrice)`
3.  **Average Price (Cost Basis)**: `TotalBuyCost / TotalBuyQuantity`
4.  **Market Value**: `NetHoldings * CurrentMarketPrice`
5.  **Total Cost**: `NetHoldings * AveragePrice`
6.  **Total Return**: `MarketValue - TotalCost`
7.  **Total Return %**: `(TotalReturn / TotalCost) * 100`

---

## Removal Constraints

To maintain data integrity, a position can only be removed if:
- It has **no active Alert Rules**.
- The removal operation deletes the `WatchlistItem` and **all associated `Trade` records** in a single transaction.

---

## Logic Highlights

| Feature | Detail |
|---|---|
| **Transaction Safety** | Uses `ExecuteTransactionAsync` to ensure trades and watchlist entries are in sync. |
| **Validation** | Prevents "Short Selling" by validating net holdings before a `Sell` trade is recorded. |
| **Bulk Import** | Supports importing multiple positions via `BulkImportPositionsAsync` for easy migration. |
| **Real-Time Enrichment** | Integrates with `IStockDataService` to provide live P&L (Profit and Loss) metrics. |
