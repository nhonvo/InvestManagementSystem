---
description: Seq logging guidance: request/response logging, correlation, and event-flow logging standards (API → SNS/SQS → Worker).
type: reference
status: draft
version: 1.0
tags: [seq, logging, correlation, tracing, request-response, worker, inventoryalert]
last_updated: 2026-04-28
---

# Seq: Request/Response + Flow Logging Standard (InventoryAlert)

This doc is based on the current implementation in branch `refactor/observability-and-logging`, and expands it with recommended standards for:

- request-level logs (HTTP),
- event-flow logs (publish/consume/handler),
- safe request/response body logging (size + redaction),
- how to query Seq to trace a full flow.

---

## 1) What is implemented now (observed)

### 1.1 API correlation id

File: `InventoryManagementSystem/InventoryAlert.Api/Middleware/CorrelationIdMiddleware.cs`

- Header used: `X-Correlation-Id`
- Precedence: incoming header → new `Guid`
- Stored in:
  - `HttpContext.Items["X-Correlation-Id"]`
  - response header `X-Correlation-Id`
  - Serilog `LogContext` property: `CorrelationId`
- Also sets the shared correlation provider:
  - `ICorrelationProvider.SetCorrelationId(correlationId)`

### 1.2 One structured “request completion” log per HTTP request

File: `InventoryManagementSystem/InventoryAlert.Api/Middleware/PerformanceMiddleware.cs`

Emits exactly one log event with fields:

- `Method`, `Path`, `StatusCode`, `ElapsedMs`
- `UserId` (or `Anonymous`)
- `CorrelationId` (CID)

Levels:

- `Error` for `StatusCode >= 500`
- `Warning` for slow requests (`ElapsedMs > 500`)
- else `Information`

### 1.3 Serilog service identity fields

File: `InventoryManagementSystem/InventoryAlert.Api/Program.cs`

Enriches:

- `Service = InventoryAlert.Api`
- `Environment = ASPNETCORE_ENVIRONMENT`

Worker has similar Serilog bootstrap and should also include `Service = InventoryAlert.Worker` (verify/keep consistent).

### 1.4 API → SNS publish includes CorrelationId

File: `InventoryManagementSystem/InventoryAlert.Api/Services/EventService.cs`

- Stamps `EventEnvelope.CorrelationId = _correlationProvider.GetCorrelationId()`

File: `InventoryManagementSystem/InventoryAlert.Infrastructure/Messaging/SnsEventPublisher.cs`

- Publishes SNS MessageAttributes:
  - `EventType`
  - `Source`
  - `CorrelationId`
- Emits publish log:
  - `[SnsEventPublisher] Published {EventType} | CorrelationId={CorrelationId} | SnsMessageId={SnsId}`

### 1.5 Correlation provider supports async/worker contexts

File: `InventoryManagementSystem/InventoryAlert.Infrastructure/Utilities/CorrelationProvider.cs`

- Uses `AsyncLocal<string?>` to store correlation id when explicitly set.
- Falls back to HttpContext Items.
- Generates a new Guid as last resort.

---

## 2) Standard log event types (recommended)

Use stable templates so Seq queries are predictable. Prefer structured fields over text parsing.

### 2.1 HTTP request completion (already implemented)

Event name (conceptual):

- `http.completed`

Fields:

- `Service`, `Environment`
- `CorrelationId`
- `Method`, `Path`, `StatusCode`, `ElapsedMs`
- `UserId` (when authenticated)

### 2.2 Event publish

Event name:

- `event.published`

Where:

- API publisher (`SnsEventPublisher`)

Fields:

- `CorrelationId`
- `EventType`
- `Source`
- `SnsMessageId`
- optional: `EnvelopeMessageId` (if you set one), `TenantId`, `UserId`

### 2.3 Event consumed (SQS)

Event name:

- `event.consumed`

Where:

- Worker poller (just after successful envelope parse)

Fields:

- `CorrelationId`
- `EventType`
- `MessageId` (envelope) and/or SQS `MessageId`
- `ApproximateReceiveCount`

### 2.4 Handler/job start + completion

Event names:

- `handler.started`
- `handler.completed`

Fields:

- `CorrelationId`
- `EventType`
- `HandlerName` / `JobName`
- `Succeeded`
- `ElapsedMs`
- `ErrorCode` (when failed)

---

## 3) Safe request/response body logging (recommended; not yet implemented)

Body logging is the fastest way to explode Seq storage/cost. The standard should be:

- Off by default
- Enabled only for:
  - errors (`StatusCode >= 400`) and/or
  - a short-lived debug toggle

### 3.1 Redaction rules (minimum)

Never log:

- `Authorization` header
- cookies / refresh token cookie
- passwords, secrets, API keys
- JWTs

If you log JSON bodies, redact common fields:

- `password`, `token`, `accessToken`, `refreshToken`, `apiKey`, `secret`, `key`

### 3.2 Truncation rules

- Request body max length: 2–8 KB
- Response body max length: 2–8 KB
- Add:
  - `RequestBodyTruncated=true|false`
  - `ResponseBodyTruncated=true|false`

### 3.3 When to log bodies

Recommended default:

- log bodies only when:
  - `StatusCode >= 400`, OR
  - a header is present: `X-Debug-Log-Bodies: true` (dev only), OR
  - an allowlist of endpoints.

### 3.4 Alternative: payload reference

If you truly need complete bodies:

- store the full body in a secure store keyed by `CorrelationId`
- store only a `PayloadRef` in Seq

---

## 4) How to trace one flow in Seq (recommended queries)

### 4.1 Primary key: CorrelationId

Start with:

- filter by `CorrelationId = "<value>"`.

You should expect to see:

1) `http.completed` from API
2) `event.published` from API publisher
3) `event.consumed` from Worker
4) `handler.*` logs from Worker/Hangfire handlers

### 4.2 Secondary keys

If CorrelationId is missing for older events, pivot by:

- `SnsMessageId`
- `EventType`
- envelope `MessageId`
- user/domain ids (`UserId`, `AlertRuleId`, `Symbol`)

---

## 5) Minimum “flow logging” checklist (what each layer must log)

### API

- `http.completed` for every request (done)
- `event.published` whenever publishing to SNS (done)
- `CorrelationId` included in event envelope (done)

### Worker

- `event.consumed` when a message is parsed (add if not present)
- handler/job start + completion with `Succeeded` + `ElapsedMs`
- ensure `CorrelationId` is pushed into `LogContext` per envelope

### UI (optional)

Do not send browser logs directly to Seq.

If needed:

- UI → API telemetry endpoint (sanitized)
- API enriches with `CorrelationId` and forwards to Seq

