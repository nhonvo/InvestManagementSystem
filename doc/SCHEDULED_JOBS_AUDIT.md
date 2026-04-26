# Scheduled Jobs Flow Audit

This document breaks down the execution steps and dependencies of all background jobs in `InventoryAlert.Worker`.

## 1. SyncPricesJob
**Purpose:** Fetches real-time price quotes for all active products.
1. **Fetch Symbols:** Retrieves all product tickers from `IStockListingRepository`.
2. **Parallel Fetch:** Iterates through tickers and calls `IFinnhubClient.GetQuoteAsync(symbol)` in parallel (limited concurrency).
3. **Record History:** Collects all quotes and performs a bulk insert via `AddRangeAsync` into `PriceHistory`.
4. **Check Alerts:** 
    - Fetches all active `AlertRules` for the processed symbols in a single batch.
    - Evaluates rules with a memoized trade basis cache to avoid redundant queries.
    - Queues `Notification` objects.
5. **Notify:** Performs bulk insert of notifications and dispatches external alerts via `IAlertNotifier`.
6. **Persistence:** Persists all state changes in a single transaction via `SaveChangesAsync`.

## 2. ProcessQueueJob

**Purpose:** Processes generic background tasks from a local concurrent queue.

1. **Dequeue:** Polling `IBackgroundTaskQueue` for the next `Func<CancellationToken, ValueTask>`.
2. **Execute:** Invokes the work item within a scoped `IServiceProvider`.
3. **Log:** Captures success/failure of ad-hoc tasks.

## 3. SqsScheduledPollerJob

**Purpose:** Bridges AWS SQS messages to the internal processing queue.

1. **Long Poll:** Calls `ISqsHelper.ReceiveMessagesAsync` (configured for short/long polling).
2. **Batch Processing:** For each message:
    - Identifies the job type.
    - Enqueues to `IBackgroundTaskQueue`.
    - Deletes message from SQS upon successful enqueue.

## 4. CleanupPriceHistoryJob

**Purpose:** Database maintenance for the `PriceHistory` table.

1. **Define Threshold:** Calculates cutoff date (e.g., `DateTime.UtcNow.AddDays(-30)`).
2. **Batch Delete:** Executes bulk delete on `PriceHistory` records older than the threshold.
3. **Rationale:** With a 15-minute sync interval for 1,000+ symbols, the table grows by ~35M rows/year. Cleanup prevents unbounded storage growth and maintains index performance for historical charts (which capped at 1-year range).

## 5. CompanyNewsJob

**Purpose:** Syncs latest market news for tracked symbols.

1. **Get Targets:** Lists tickers from `IProductRepository`.
2. **Fetch News:** Calls `IStockDataService.GetCompanyNewsAsync` for each symbol.
3. **Filter:** Removes duplicates already present in the local database.
4. **Store:** Inserts new news items into `ICompanyNewsRepository`.

## 6. SyncMetricsJob

**Purpose:** Syncs fundamental metrics (P/E ratio, Market Cap, etc.).

1. **Get Symbols:** Identifies tracked products.
2. **External Call:** Calls `IStockDataService.GetMetricsAsync`.
3. **Update Entity:** Maps metrics to the `Product` entity fundamental fields.
4. **Persist:** Updates the database records.

## 7. SyncEarningsJob

**Purpose:** Tracks historical and estimated earnings data.

1. **Iterate Tickers:** Standard loop through active symbols.
2. **Fetch:** `IStockDataService.GetEarningsAsync`.
3. **Merge:** Upserts earnings records into `IEarningsRepository`.

## 8. SyncRecommendationsJob

**Purpose:** Captures analyst buy/sell recommendation trends.

1. **Fetch:** Calls `IStockDataService.GetRecommendationsAsync`.
2. **Aggregate:** Stores the latest "Strong Buy", "Hold", etc., counts.
3. **Archive:** Keeps a history of recommendation shifts.

## 9. SyncInsidersJob

**Purpose:** Monitors insider trading activities.

1. **Fetch:** Calls `IStockDataService.GetInsidersAsync`.
2. **Deduplicate:** Checks against existing transaction IDs.
3. **Alert:** (If configured) Enqueues an alert if a high-volume insider sale occurs.

---

## Technical Observations for Refactoring

| Issue                | Observation                                                              | Recommended Fix                                                           |
| :------------------- | :----------------------------------------------------------------------- | :------------------------------------------------------------------------ |
| **API Throttling**   | Most "Sync" jobs iterate and call Finnhub per symbol.                    | Implement Batching or Rate-Limiting middleware in the `FinnhubClient`.    |
| **Transactionality** | Bulk updates in `SyncPricesJob` use `UpdateRangeAsync`.                  | Ensure `IUnitOfWork` is used to prevent partial updates.                  |
| **Redundancy**       | Every job manually fetches symbols from `IProductRepository`.            | Create a `BaseSyncJob` that handles symbol resolution and error logging.  |
| **Queue Saturation** | `SqsScheduledPollerJob` enqueues to the same queue as `ProcessQueueJob`. | Use prioritized queues or separate workers for high-priority price syncs. |
