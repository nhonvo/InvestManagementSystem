# 🔍 Code Review & Technical Debt (TODOs)

This document summarizes the architectural, security, and performance gaps identified during a deep code review. Inline `// TODO: [Enhancement]` and `// TODO: [Bug]` comments have been injected directly into the source code to track these issues alongside the implementation.

## 1. Security & Authentication

- **Hardcoded Fallback Credentials (`AuthController.cs`)**: If the `Auth:Username` and `Auth:Password` settings are missing from the configuration (e.g., missed environment variables in Docker), the system currently falls back to `admin`/`admin123`. This exposes the application to unintended access.
  - *Action Required*: Remove the fallback. Throw a startup exception or return a 500 error if secure credentials are not supplied via configuration.

## 2. Concurrency & Atomicity

- **Lost Update Hazard (`ProductService.cs - UpdateStockCountAsync`)**: The method requests the product entity using `GetByIdAsync`, mutates the stock count, and then saves it via `UpdateAsync`. Any concurrent modifications to other properties (e.g., the background background price sync mutating `CurrentPrice`) will be blindly overwritten.
  - *Action Required*: Use an intentional update technique like explicit column updating (`ExecuteUpdateAsync`), or introduce optimistic concurrency (`RowVersion` / Concurrency Token) on the `Product` entity.
- **Race Condition in Message Dedup (`PollSqsJob.cs`)**: The deduplication logic uses `GetStringAsync()` to check for previous processing, and then later (outside the critical path) uses `SetStringAsync()` to persist it. This creates a race condition for duplicates arriving simultaneously.
  - *Action Required*: Use an atomic operation like Redis `SET ... NX` (Set Not Exists).

## 3. Validation & Edge Cases

- **Bulk Insert Validation (`ProductService.cs - BulkInsertProductsAsync`)**:
  - *Issue 1*: FluentValidation does not automatically validate the internal elements of an `IEnumerable<T>` payload at the controller level unless explicitly wired up with `RuleForEach`. The current implementation trusts the bulk payload completely.
  - *Issue 2*: There is no guard tracking empty lists (`!requests.Any()`). Passing an empty array executes a pointless database transaction.
  - *Action Required*: Implement a manual validator pass or a collection wrapper for validation, and add a quick empty guard.

## 4. Performance & Scalability (Worker)

- **N+1 External IO inside Loops (`SyncPricesJob.cs`, `EarningsCheckJob.cs`, `NewsCheckJob.cs`, `InsiderTxCheckJob.cs`)**: Each Hangfire background worker retrieves all products using `ToListAsync()` pulling an unbounded dataset into memory, followed by executing a sequential Finnhub HTTP API call `await Fetch(...)` per product inside a `foreach` loop. If there are thousands of products, this will lead to unbounded memory allocation, prolonged job durations, blocking threads, and immediate violation of Finnhub's generic rate limits (60 requests/min).
  - *Action Required*: Change `ToListAsync()` to database batching, implement a parallel fan-out API caller (`Task.WhenAll`), and employ `SemaphoreSlim` and `Task.Delay` logic to enforce Finnhub's rate limit.

## 5. Configuration Defaults

- **Hardcoded Job CRON (`JobSchedulerService.cs`)**: The price sync Hangfire job is scheduled with a hardcoded `"*/10 * * * *"` cron expression (every 10 minutes). It completely ignores the `MinuteSyncCurrentPrice` config parameter available in `appsettings.json`.
  - *Action Required*: Inject `IConfiguration` into the JobSchedulerService, dynamically calculate the CRON string based on the configuration value.
- **Hardcoded Cache TTL (`ProductService.cs`)**: `GetProductByIdAsync` sets the cache TTL permanently to 10 minutes.
  - *Action Required*: Move the TTL interval to `appsettings.json`.

## 6. Observability & Logging

- **Missing Liveness Probes (`Program.cs`)**: In containerized environments like Docker or Kubernetes, there is no way for the orchestrator to know if the C# application is actually healthy, routing traffic normally, or silently hung in a deadlock.
  - *Action Required*: Integrate ASP.NET HealthChecks (`builder.Services.AddHealthChecks()`) and map an endpoint `app.MapHealthChecks("/health")`.
- **Inappropriate Logging Strategy (`FinnhubClient.cs`)**: Error logging writes directly to STDOUT using `Console.WriteLine()`, bypassing the highly configurable Serilog provider.
  - *Action Required*: Inject `ILogger<FinnhubClient>` and write structural logs using standard `_logger.LogError(...)`.
- **Leaked Capture Variable Warning (`SyncPricesJob.cs`)**: The class constructor injects `cache` while mapping it to `_cache`. However, the variable `cache` is referenced explicitly at line 62, creating a compiler state capture warning (CS9124).
  - *Action Required*: Convert the direct parameter reference to the internal `_cache` field reference.
