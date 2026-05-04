# Logging Review & Recommendations

Scope: repository-wide review focused on **API/Worker request flow tracing**, **request/response logging**, and **centralized structured logging**. No code changes included in this document.

Date: 2026-04-26

---

## 1) Current State (What Exists Today)

### API (`InventoryManagementSystem/InventoryAlert.Api`)

- **Serilog bootstrap** configured in `InventoryManagementSystem/InventoryAlert.Api/Program.cs` (Console + Seq + rolling File).
- **Correlation ID** middleware exists: `InventoryManagementSystem/InventoryAlert.Api/Middleware/CorrelationIdMiddleware.cs`
  - Header: `X-Correlation-Id`
  - Behavior: uses incoming header if present; otherwise generates a `Guid`.
  - Adds the id to:
    - `context.Items["X-Correlation-Id"]`
    - `context.Response.Headers["X-Correlation-Id"]`
    - Serilog `LogContext` property: `CorrelationId`
- **Exception logging** exists: `InventoryManagementSystem/InventoryAlert.Api/Middleware/GlobalExceptionMiddleware.cs`
- **Request timing logs** exist: `InventoryManagementSystem/InventoryAlert.Api/Middleware/PerformanceMiddleware.cs`

### Worker (`InventoryManagementSystem/InventoryAlert.Worker`)

- Serilog bootstrap exists in `InventoryManagementSystem/InventoryAlert.Worker/Program.cs` (Console + Seq).
- SQS processing uses scopes + Serilog context:
  - `InventoryManagementSystem/InventoryAlert.Worker/ScheduledJobs/ProcessQueueJob.cs`
  - Pushes `CorrelationId` into:
    - `ILogger.BeginScope(...)` dictionary (`MessageId`, `EventType`, `CorrelationId`)
    - Serilog `LogContext` property `CorrelationId`

### Infrastructure (`InventoryManagementSystem/InventoryAlert.Infrastructure`)

- Messaging publisher logs publishes:
  - `InventoryManagementSystem/InventoryAlert.Infrastructure/Messaging/SnsEventPublisher.cs`
- `ICorrelationProvider` currently reads **only** from `HttpContext`:
  - `InventoryManagementSystem/InventoryAlert.Infrastructure/Utilities/CorrelationProvider.cs`

### UI (`InventoryAlert.UI`)

- No explicit propagation of `X-Correlation-Id` found in UI sources (search for `X-Correlation-Id` / `CorrelationId` returned no matches).

---

## 2) What’s Missing vs Your Goal

You asked for:

1) **Log request + response** (ideally bodies), but avoid exploding log size.
2) A **single id** to follow the flow end-to-end.
3) A way to **centralize** the logging (avoid “log sprinkled everywhere”).
4) Identify parts that **still have little/no logging**.

Key gaps found:

### Gap A — API → Worker correlation is not reliable today

- `README.md` claims: “Every request includes a `CorrelationId` that propagates from the API through SNS/SQS to the Worker.”
- However `InventoryManagementSystem/InventoryAlert.Api/Services/EventService.cs` builds `EventEnvelope` without setting `CorrelationId`.
  - `InventoryManagementSystem/InventoryAlert.Domain/Events/EventEnvelope.cs` defaults `CorrelationId` to `string.Empty`.
  - `InventoryManagementSystem/InventoryAlert.Infrastructure/Messaging/SnsEventPublisher.cs` only includes the `CorrelationId` attribute if it’s not empty.

Impact:

- For request-triggered async flows (ex: `POST /api/v1/events`), the Worker may log without the same `CorrelationId` as the originating HTTP request, making “search by id” unreliable.

### Gap B — Request/response body logging is not implemented

- The API logs *completion* timing (method/path/status/time), but does not log:
  - Request headers
  - Request body
  - Response headers
  - Response body

### Gap C — Entry-point visibility is low in API controllers/services

Findings from code scan:

- All API controllers have **no `ILogger` injection** and **no `.Log*()` calls:
  - `InventoryManagementSystem/InventoryAlert.Api/Controllers/*.cs`
- Several API services have no logs:
  - `InventoryManagementSystem/InventoryAlert.Api/Services/EventService.cs`
  - `InventoryManagementSystem/InventoryAlert.Api/Services/AuthService.cs`
  - `InventoryManagementSystem/InventoryAlert.Api/Services/AlertRuleService.cs`
  - `InventoryManagementSystem/InventoryAlert.Api/Services/NotificationService.cs`

Note: this doesn’t mean they *must* log everywhere; it means the **only consistent view** of API activity is currently the middleware timing log (plus error logs).

### Gap D — Correlation provider works for HTTP only

- `InventoryManagementSystem/InventoryAlert.Infrastructure/Utilities/CorrelationProvider.cs` depends on `IHttpContextAccessor`.
- In the Worker (no HTTP request), this returns a **new Guid**, which breaks continuity if Worker code publishes further messages and expects to “inherit” correlation.

---

## 3) Recommended Logging Design (Centralized + Searchable + Size-Controlled)

This is the recommended model:

### 3.1 Use one “Flow Id” everywhere

Keep `CorrelationId` as the primary flow identifier:

- HTTP: `X-Correlation-Id` header + `LogContext.CorrelationId`
- Messaging: `EventEnvelope.CorrelationId` + SNS/SQS message attribute `CorrelationId`
- Worker: push envelope `CorrelationId` into scope + `LogContext`

Also consider adding (optional, but valuable):

- `TraceId` (W3C tracing / `Activity.TraceId`) for distributed tracing later
- `RequestId` (`HttpContext.TraceIdentifier`) for server-side uniqueness

### 3.2 Centralize “HTTP request completion log” (cheap, always-on)

Goal: every request creates exactly **1 structured log event** at the end:

Fields to include (recommended):

- `CorrelationId`
- `Method`, `Path`, `StatusCode`, `ElapsedMs`
- `UserId` (if authenticated), `ClientIp`
- `RouteTemplate` / `EndpointName` (if available)
- `RequestContentLength`, `ResponseContentLength` (when known)

This gives you:

- small logs
- consistent format
- easy searching in Seq

### 3.3 Add request/response body logging only under strict controls (expensive, opt-in)

Body logging is where log volume and sensitive data risk skyrockets. Recommended controls:

**Controls (must-have)**

- Feature flag (enable in Dev/Docker only, off in Prod by default)
- Content-type allowlist (`application/json`, maybe `text/*`)
- Size limits (log first N bytes only; store `Truncated=true`)
- Redaction (passwords/tokens/PII)
- Endpoint allowlist/denylist
  - Denylist examples: `POST /api/v1/auth/login`, refresh-token endpoints

**When to log bodies**

- Errors only (responses with `StatusCode >= 400`), plus explicit debug scenarios
- Or sample rate (ex: 1% of successful traffic)

**Alternative if you need full bodies**

- Write full bodies to a separate secure store (blob/file) keyed by `CorrelationId`, and keep Seq logs lightweight:
  - Seq event contains `CorrelationId` + a `PayloadRef` (path/key)

### 3.4 Centralize “business event logs” (domain-level, not request-level)

For meaningful actions (create alert rule, register user, enqueue event, etc.), log:

- event name (stable template)
- important identifiers (`UserId`, `AlertRuleId`, `Symbol`)
- `CorrelationId`

This should live at service-layer boundaries (not in every controller).

---

## 4) Concrete Fix List (Prioritized, Minimal, High Value)

### P0 — Make correlation actually propagate API → SNS/SQS → Worker

Acceptance criteria:

- Any HTTP request that publishes an event results in:
  - API logs containing `CorrelationId`
  - SNS publish logs containing the same `CorrelationId`
  - Worker processing logs containing the same `CorrelationId`

Candidate locations to stamp `EventEnvelope.CorrelationId`:

- `InventoryManagementSystem/InventoryAlert.Api/Services/EventService.cs` (inject `ICorrelationProvider`)
- or a decorator around `IEventPublisher` that stamps missing correlation ids in one place

### P0 — Add “one request completion log” with consistent fields

Acceptance criteria:

- Every request generates exactly one completion log event with consistent fields.
- Slow requests (like current `PerformanceMiddleware`) are still visible (either by a `Slow=true` flag or level/threshold).

### P1 — Add request/response body logging safely (debug-only)

Acceptance criteria:

- Body logging:
  - is off by default
  - has truncation + redaction
  - can be toggled per environment
  - can be enabled per endpoint for targeted debugging

### P1 — Improve Worker correlation continuity for downstream publishes

Acceptance criteria:

- When handling an envelope with `CorrelationId=X`, any downstream publish/enqueue inside the same job uses `CorrelationId=X` by default.

Implementation options (choose one):

- Pass correlation explicitly as a parameter where enqueuing/publishing occurs.
- Introduce an AsyncLocal-based correlation context (set in HTTP middleware and in Worker message processing) and have `ICorrelationProvider` read from it.
- Adopt W3C tracing (`Activity`) and derive `CorrelationId` from `Activity.TraceId`.

### P2 — Add service identity properties to logs

Acceptance criteria:

- Seq can filter by service quickly:
  - `Service = 'InventoryAlert.Api'`
  - `Service = 'InventoryAlert.Worker'`

### P2 — Reduce “big payload” log risk in external client logging

`InventoryManagementSystem/InventoryAlert.Infrastructure/External/Finnhub/FinnhubClient.cs` logs full response content in some warnings/errors.

Acceptance criteria:

- External-client failures log:
  - status code, endpoint/path, small excerpt (max N chars), and `CorrelationId`
  - never log entire large bodies by default

---

## 5) Coverage Findings (Where Logging Is Thin Today)

### API controllers (no direct logging)

All controllers found without `ILogger` and without `.Log*()`:

- `InventoryManagementSystem/InventoryAlert.Api/Controllers/AlertRulesController.cs`
- `InventoryManagementSystem/InventoryAlert.Api/Controllers/AuthController.cs`
- `InventoryManagementSystem/InventoryAlert.Api/Controllers/EventsController.cs`
- `InventoryManagementSystem/InventoryAlert.Api/Controllers/MarketController.cs`
- `InventoryManagementSystem/InventoryAlert.Api/Controllers/NotificationsController.cs`
- `InventoryManagementSystem/InventoryAlert.Api/Controllers/PortfolioController.cs`
- `InventoryManagementSystem/InventoryAlert.Api/Controllers/StocksController.cs`
- `InventoryManagementSystem/InventoryAlert.Api/Controllers/WatchlistController.cs`

Recommendation:

- Keep controllers thin, but ensure service-level “business event logs” exist for key actions, and HTTP middleware provides the consistent request-completion log.

### API services with little/no logging

- `InventoryManagementSystem/InventoryAlert.Api/Services/EventService.cs` (also the correlation stamping gap)
- `InventoryManagementSystem/InventoryAlert.Api/Services/AuthService.cs` (login/register activity visibility is low)
- `InventoryManagementSystem/InventoryAlert.Api/Services/AlertRuleService.cs`
- `InventoryManagementSystem/InventoryAlert.Api/Services/NotificationService.cs`

### Worker area with no logs

- `InventoryManagementSystem/InventoryAlert.Worker/Hosting/BackgroundTaskQueue.cs` (may be fine, but hard to debug queue backpressure without any counters/logs)

---

## 6) Seq Search Examples (How You’d Trace a Flow)

From `InventoryAlert.Wiki/docs/07-dev-maintenance/operational-runbook.md`, you already use:

- Errors: `@Level = 'Error'`
- Service: `@Properties.SourceContext like '%Worker%'`
- Correlation: `@Properties.CorrelationId = '<id>'`

Recommended additional searches once fields are standardized:

- A single flow: `CorrelationId = '<id>'`
- One event type: `EventType = 'inventoryalert.news.sync-requested.v1'`
- One message: `MessageId = '<id>'`
- Slow requests: `ElapsedMs >= 500` or `Slow = true`

---

## 7) Notes / Potential Inconsistencies to Validate

These aren’t strictly “logging” issues, but they affect observability and correlation:

- Worker router expects SQS `MessageAttributes["MessageType"]` in `InventoryManagementSystem/InventoryAlert.Worker/IntegrationEvents/Routing/IntegrationMessageRouter.cs`.
  - Current SNS publisher uses message attributes `EventType`, `Source`, and optional `CorrelationId` in `InventoryManagementSystem/InventoryAlert.Infrastructure/Messaging/SnsEventPublisher.cs`.
  - Consider standardizing attribute names (`EventType` vs `MessageType`) and ensuring Worker routing matches what the publisher emits.

---

## 8) Suggested Next Output (If You Want)

If you want, I can produce a second markdown file with a concrete “implementation checklist” (exact middleware/filter approach, redaction rules, size limits, and an example Seq dashboard/query set). This would still be documentation-only unless you ask for code changes.

