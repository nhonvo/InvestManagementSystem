# Hangfire Monitoring

> How to use the Hangfire Dashboard to inspect and manage background jobs.

## Accessing the Dashboard

Navigate to `http://localhost:8080/hangfire` (requires **Admin** role JWT).

## Dashboard Sections

| Section | Purpose |
|---|---|
| **Enqueued** | Jobs waiting to be picked up by a worker |
| **Processing** | Jobs currently executing |
| **Succeeded** | Jobs that completed successfully |
| **Failed** | Jobs that threw an unhandled exception |
| **Recurring** | Cron-scheduled jobs and their next run time |

## Scheduled Jobs Visible in Dashboard

| Job Name | Cron | Failure Impact |
|---|---|---|
| `SyncPricesJob` | `*/15 * * * *` | No price update; no alert evaluation for that cycle |
| `SyncMetricsJob` | `0 6 * * *` | Stale `StockMetric` data (P/E, EPS, margins) |
| `SyncEarningsJob` | `0 7 * * *` | Stale `EarningsSurprise` data |
| `SyncRecommendationsJob` | `0 8 * * 1` | Stale analyst consensus |
| `SyncInsidersJob` | `0 8 * * *` | Stale insider transaction data |
| `CompanyNewsJob` | `0 */6 * * *` | Missing company news for that window |
| `MarketNewsJob` | `0 */2 * * *` | Missing general market news |
| `CleanupPriceHistoryJob` | `@daily` | `PriceHistory` table grows unbounded |
| `ProcessQueueJob` | Continuous | SQS events back up until next `SqsScheduledPollerJob` runs |

## Retrying a Failed Job

1. Click **Failed** in the left sidebar
2. Find the failed job entry
3. Click **Retry** to requeue it, or **Delete** to discard

## Triggering a Manual Price Sync

From Hangfire Dashboard:
1. Go to **Recurring Jobs**
2. Find `SyncPricesJob`
3. Click **Trigger Now**

Or via the API:
```bash
POST http://localhost:8080/api/v1/stocks/sync
Authorization: Bearer <admin-jwt>
```
