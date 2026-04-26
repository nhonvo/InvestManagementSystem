---
description: Integration test plan focused on Worker/Hangfire jobs and SQS/DLQ (Option C recovery) flows.
type: reference
status: draft
version: 1.0
tags: [tests, integration-tests, worker, hangfire, sqs, dlq, option-c, inventoryalert]
last_updated: 2026-04-26
---

# Jobs + Worker Integration Test Plan (with SQS/DLQ Option C)

This plan complements `InventoryManagementSystem/InventoryAlert.IntegrationTests/INTEGRATION_TEST_PLAN.md` by focusing specifically on:

- Hangfire recurring jobs (Worker)
- SQS consumption semantics (success = delete, failure = redeliver)
- DLQ behavior (redrive policy)
- **Option C** recovery: a Hangfire “DLQ reprocessor/redrive” job that operators can retry in the Hangfire Dashboard

This is documentation only; it describes what to test and how to structure it.

---

## 1) Test environments (recommended)

### Local dev

Use the existing docker compose stack under:

- `InventoryManagementSystem/` (API + Worker + DB + Redis + Moto/LocalStack as configured)

### CI

Current `.github/workflows/ci.yml` already:

- builds .NET + runs unit tests
- starts Docker compose
- runs `InventoryAlert.IntegrationTests`

Extend your integration tests in a way that does not require flaky timing or long waits.

---

## 2) Core invariants to enforce (must-have)

### 2.1 Success path

- Given an event message in the main queue:
  - Worker processes it exactly once (idempotent behavior).
  - Worker deletes the SQS message.
  - Expected side effects happen (DB rows / notifications / logs).

### 2.2 Failure path → retry by redelivery

- Given a poison message or a handler that throws:
  - Worker does not delete the message.
  - SQS redelivers it after visibility timeout.

Important: today the Redis dedup behavior can cause “failure then ACK on redelivery” (message loss). If Option C is the direction, adjust production code first so the test can enforce “retries are real”.

### 2.3 Failure path → DLQ after maxReceiveCount

- Given repeated failures:
  - the message eventually appears in the DLQ (based on queue redrive policy)
  - the message stops showing up in the main queue

In local dev, `moto-init` creates `event-queue` with `maxReceiveCount=3`.

---

## 3) Test categories

### 3.1 Worker job tests (direct invocation)

Pattern:

- Resolve a job from DI and call `ExecuteAsync()`.
- Assert the side effects (DB, cache, outgoing events).

Targets:

- `SyncPricesJob`
- `NewsSyncJob`
- `CleanupPriceHistoryJob`

Guidance:

- Keep these tests deterministic: mock external APIs (Finnhub) or use a predictable stub.
- Avoid long-running loops; test the single execution method.

### 3.2 SQS consumer tests (message-driven)

Pattern:

- Publish a known message into SQS.
- Run a single “poll + process” unit (preferably one `ProcessBatchAsync` call, not an infinite loop).
- Assert:
  - message deleted (success) OR message still present (failure)
  - side effects

If the current architecture only exposes the infinite loop, consider adding a test seam (already present: `ProcessBatchAsync` in `ProcessQueueJob`).

### 3.3 DLQ reprocessor job tests (Option C)

This is what makes “Retry in Hangfire Dashboard” meaningful for failed message recovery.

Test cases:

1) **Reprocess DLQ batch (success)**
   - Arrange: message exists in DLQ
   - Act: run `DlqReprocessorJob.ExecuteAsync(max=10)`
   - Assert:
     - message is published back to main queue (or SNS, depending on design)
     - DLQ message is deleted only after publish succeeds

2) **Reprocess DLQ batch (partial failure)**
   - Arrange: DLQ contains both valid and invalid messages
   - Act: run job
   - Assert:
     - valid messages are replayed
     - invalid messages are either left in DLQ or moved aside (depending on policy)
     - job returns a result/log summary (counts)

3) **Idempotency / replay safety**
   - Arrange: same DLQ message is processed twice (simulate crash before delete)
   - Assert: the replay operation does not create duplicate side effects (use `MessageId`-based idempotency key).

---

## 4) Suggested folder structure (inside IntegrationTests)

```text
InventoryManagementSystem/InventoryAlert.IntegrationTests/
├── Tests/
│   ├── Worker/
│   │   ├── Jobs/
│   │   │   ├── SyncPricesJobTests.cs
│   │   │   ├── NewsSyncJobTests.cs
│   │   ├── Sqs/
│   │   │   ├── SqsConsumerSuccessTests.cs
│   │   │   ├── SqsConsumerDlqTests.cs
│   │   ├── Dlq/
│   │   │   ├── DlqReprocessorJobTests.cs
```

---

## 5) Timing pitfalls + how to avoid flaky tests

- Prefer asserting via “pull once” APIs rather than sleeping:
  - check SQS queue length / receive messages
  - query DB state
- Use bounded waits with polling loops (max 10–30s) for asynchronous behaviors.
- Keep maxReceiveCount small in local env (already `3`) and set `VisibilityTimeout` low enough for tests.
- Ensure test cleanup resets SQS queues, Redis keys, and DB state between tests.

---

## 6) What to update in CI after adding these tests

Once the new tests exist:

- Confirm `.github/workflows/ci.yml` stack includes:
  - SQS + DLQ (Moto/LocalStack) with redrive policy
  - Redis (for dedup/cooldown)
- Add a CI summary section for:
  - number of messages processed
  - DLQ count (if any)
  - replay runs (if job exists)

