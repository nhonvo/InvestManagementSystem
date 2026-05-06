# Earnings Surprise Synchronization Flow

> Orchestration pipeline for fetching historical earnings performance and surprise metrics.

## Sequence

```mermaid
sequenceDiagram
    participant HF as Hangfire Scheduler
    participant Job as SyncEarningsJob
    participant Finnhub as Finnhub API
    participant DB as PostgreSQL

    HF->>Job: Trigger (Scheduled)
    Job->>DB: SELECT TickerSymbols FROM StockListing
    
    loop For each symbol
        Job->>Finnhub: GET /stock/earnings?symbol={s}
        Finnhub-->>Job: List<EarningsSurprise>
        Job->>DB: UpsertRangeAsync(Earnings)
    end
    
    Job->>DB: SaveChangesAsync()
```

## Data Points

| Field | Description |
|---|---|
| **Actual EPS** | The actual Earnings Per Share reported by the company. |
| **Estimate EPS** | The consensus analyst estimate for EPS. |
| **Surprise %** | The percentage difference between actual and estimate. |
| **Period** | The fiscal quarter/year for the reported data. |

## Implementation Details

| Feature | Detail |
|---|---|
| **Upsert Logic** | Uses `UpsertRangeAsync` to handle updates to existing periods or additions of new reports. |
| **PostgreSQL Store** | Unlike news, earnings data is stored in PostgreSQL for relational analysis and reporting. |
| **Sequential Processing** | Currently processed sequentially per symbol to respect API rate limits for non-premium tiers. |
