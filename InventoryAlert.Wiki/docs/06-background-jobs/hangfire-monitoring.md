# Hangfire Monitoring

> How to use the Hangfire Dashboard to inspect and manage background jobs.

## Accessing the Dashboard

Navigate to `http://localhost:8081/hangfire` (Docker Compose maps Worker `8080` → host `8081`).

Authorization note: the Worker dashboard uses a development-friendly authorization filter (`DevDashboardAuthorizationFilter`). It is not driven by JWT.

## Dashboard Sections

| Section | Purpose |
|---|---|
| **Enqueued** | Jobs waiting to be picked up by a worker |
| **Processing** | Jobs currently executing |
| **Succeeded** | Jobs that completed successfully |
| **Failed** | Jobs that threw an unhandled exception |
| **Recurring** | Cron-scheduled jobs and their next run time |

## Scheduled Jobs Visible in Dashboard

Recurring jobs are registered by `JobSchedulerService` and use schedules from `WorkerSettings.Schedules.*`.

| Recurring Job Id | Schedule setting | Failure impact |
|---|---|---|
| `sync-prices` | `Schedules.SyncPrices` | No price update; no scheduled alert evaluation for that cycle |
| `sync-metrics` | `Schedules.SyncMetrics` | Stale `StockMetric` data |
| `sync-earnings` | `Schedules.SyncEarnings` | Stale `EarningsSurprise` data |
| `sync-recommendations` | `Schedules.SyncRecommendations` | Stale analyst consensus |
| `sync-insiders` | `Schedules.SyncInsiders` | Stale insider transaction data |
| `news-sync` | `Schedules.MarketNews` | Stale market/company news snapshots in DynamoDB |
| `cleanup-prices` | `Schedules.CleanupPrices` | `PriceHistory` grows unbounded |

## Retrying a Failed Job

1. Click **Failed** in the left sidebar
2. Find the failed job entry
3. Click **Retry** to requeue it, or **Delete** to discard

## Triggering a Manual Price Sync

From Hangfire Dashboard:
1. Go to **Recurring Jobs**
2. Find `sync-prices`
3. Click **Trigger Now**

Or via the API endpoint (placeholder):

- `POST /api/v1/stocks/sync` currently returns `202 Accepted` but does not enqueue a Hangfire job.
