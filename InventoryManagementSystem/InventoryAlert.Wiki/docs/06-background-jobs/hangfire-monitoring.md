# Hangfire Monitoring

> How to use the Hangfire Dashboard to inspect and manage background jobs.

## Accessing the Dashboard

Navigate to `http://localhost:8080/hangfire` (requires Admin role).

## Dashboard Sections

| Section | Purpose |
|---|---|
| **Enqueued** | Jobs waiting to be picked up |
| **Processing** | Jobs currently running |
| **Succeeded** | Jobs that completed successfully |
| **Failed** | Jobs that threw an exception |
| **Recurring** | Scheduled jobs and their cron expressions |

## Retrying a Failed Job

1. Click **Failed** in the sidebar
2. Find the failed job entry
3. Click **Retry** or **Delete**

## Business Impact of Failures

If `FinnhubPricesSyncWorker` fails, no price updates occur for that cycle and alert evaluation is paused until the next successful sync.
