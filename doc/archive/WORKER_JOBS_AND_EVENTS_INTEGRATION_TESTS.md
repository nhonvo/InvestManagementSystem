---
description: Practical integration test setup for Worker jobs and event handling (SNS/SQS via Moto), aligned with current repository test harness.
type: reference
status: draft
version: 1.0
tags: [integration-tests, worker, jobs, events, sqs, sns, moto, hangfire, inventoryalert]
last_updated: 2026-04-28
---

# Worker Jobs + Event Handling Integration Tests (Practical Setup)

This repo already has a running-stack integration test harness under:

- `InventoryManagementSystem/InventoryAlert.IntegrationTests`
- `.github/workflows/ci.yml` starts `InventoryManagementSystem/docker-compose.yml` and runs those tests

This document focuses on *jobs + events* coverage that is currently missing or incomplete:

- Worker recurring jobs (Hangfire)
- API → SNS → SQS → Worker message handling
- DLQ behavior and manual recovery patterns

---

## 1) Current integration test style (observed)

The current integration tests are “black-box” tests:

- They call the API via HTTP using RestSharp clients.
- They depend on the Docker compose stack (API/Worker/DB/Redis/Moto/Wiremock).

Key files:

- Harness: `InventoryManagementSystem/InventoryAlert.IntegrationTests/Fixtures/InjectionFixture.cs`
- Base class: `InventoryManagementSystem/InventoryAlert.IntegrationTests/Abstractions/BaseIntegrationTest.cs`
- Polling helper: `InventoryManagementSystem/InventoryAlert.IntegrationTests/Helpers/WaitHelper.cs`
- Compose stack includes Moto and Wiremock: `InventoryManagementSystem/docker-compose.yml`

Note: `InventoryManagementSystem/InventoryAlert.IntegrationTests/INTEGRATION_TEST_PLAN.md` contains a “future” design (WebApplicationFactory/Testcontainers). It does not match the current RestSharp+Docker approach.

---

## 2) Local run: commands

From repo root:

```bash
cd InventoryManagementSystem
docker compose up -d --build
```

Verify health:

- API: `http://localhost:8080/health`
- Worker: `http://localhost:8081/health`
- Wiremock: `http://localhost:9091/__admin`
- Seq UI: `http://localhost:5341`

Then run integration tests:

```bash
dotnet test InventoryManagementSystem/InventoryAlert.IntegrationTests/InventoryAlert.IntegrationTests.csproj
```

---

## 3) What we should add: “Jobs” integration tests

### 3.1 Job execution tests (Worker jobs)

Goal: verify job side effects without relying on “Cron timing”.

Recommended approach (still black-box):

1) Trigger the job deterministically (preferred options):
   - expose an admin endpoint to enqueue the job (API or Worker), OR
   - call Hangfire enqueue endpoint if you add one, OR
   - publish an event that the worker maps into a job (ex: news sync requested).
2) Assert side effects via API endpoints (read models) and/or DB queries (if you add DB access to tests).
3) Use `WaitHelper.WaitForConditionAsync` with bounded timeouts.

Candidate jobs + suggested assertions:

- `NewsSyncJob`:
  - trigger via `POST /api/v1/events` (news sync requested)
  - assert `GET /api/v1/market/news?...` returns non-empty
- `SyncPricesJob`:
  - trigger (needs a deterministic trigger endpoint/event)
  - assert prices or notifications changed
- `CleanupPriceHistoryJob`:
  - seed data → trigger cleanup → assert old entries removed

### 3.2 Hangfire visibility tests

Goal: confirm jobs enqueue + complete/fail.

If you add a small Worker API wrapper endpoint (recommended), tests can:

- query “job status” by returned job id
- or query a “job summary” endpoint

Avoid scraping Hangfire dashboard HTML in tests.

---

## 4) What we should add: “Events” integration tests (SNS/SQS via Moto)

### 4.1 Why this matters

The most fragile path is:

API request → `EventEnvelope` publish → SNS → SQS → Worker poller → routing/handler → DB/notification

We need tests that confirm:

- envelope is stamped with `CorrelationId`
- message is delivered to SQS
- worker consumes it and routes correctly

### 4.2 Practical test strategy (host → Moto on localhost:5000)

The docker stack exposes Moto:

- `http://localhost:5000`

Moto-init provisions:

- `event-queue`
- `inventory-event-dlq`
- SNS topic and subscription

Recommended enhancements for integration tests:

- Add a small SQS client in the test project using `AWSSDK.SQS` and configure:
  - Service URL: `http://localhost:5000`
  - Region: `us-east-1`
  - Dummy credentials (`test`/`test`)

Then tests can:

1) Publish an event via API (HTTP).
2) Receive a message directly from `event-queue` and assert:
   - body is valid `EventEnvelope`
   - `CorrelationId` is non-empty
   - optional: SNS MessageAttributes include `EventType`, `CorrelationId`
3) (Optionally) re-inject the message to simulate replay.

### 4.3 DLQ behavior tests

Prerequisite: worker must not ACK messages that fail at poller/router stage.

Test:

- Send a deliberately malformed envelope or unknown `EventType`.
- Wait until it reaches DLQ (based on `maxReceiveCount`).
- Assert message appears in `inventory-event-dlq`.

Note: if the worker ACKs “unknown event” as success, it will never reach DLQ; decide policy first.

---

## 5) Required logging for test debuggability

To debug failing tests quickly, the stack should emit logs with:

- `CorrelationId`
- `EventType`
- SQS message id / envelope message id
- handler/job name

Reference:

- `doc/SEQ_REQUEST_RESPONSE_AND_FLOW_LOGGING.md`

---

## 6) Recommended additions to `InventoryAlert.IntegrationTests` (structure)

Add a worker/events test area:

```text
InventoryManagementSystem/InventoryAlert.IntegrationTests/Tests/
├── Worker/
│   ├── Events/
│   │   ├── EventPublishAndConsumeTest.cs
│   │   ├── EventDlqTest.cs
│   ├── Jobs/
│   │   ├── NewsSyncJobTest.cs
│   │   ├── SyncPricesJobTest.cs
```

And add clients/helpers:

- `Clients/SqsClient.cs` (Moto endpoint)
- `Helpers/SeqQueryHelper.cs` (optional; keep lightweight)

