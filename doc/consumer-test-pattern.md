# 🧪 Integration Test Setup Guide (InventoryAlert Pattern)

## Purpose

This document explains the "Three-Tier" integration testing architecture for the InventoryAlert project. This pattern ensures high coverage by separating HTTP surface testing, background job processing, and direct business logic (handlers) into distinct, optimized feedback loops.

---

## 🏗️ Test Architecture Summary

### 1. `InventoryAlert.Api.Tests` (Refactored IntegrationTests)
**Target:** The HTTP surface and middleware pipeline of the API.
- **Style:** Black-box. Sends real HTTP requests to the `inventory-api` container.
- **Validation:** HTTP response codes, JSON payloads, and **Docker container logs**.
- **Dependencies:** Requires the `inventory-api` container to be running.
- **Best for:** Verifying Auth middleware, routing, status codes, and the full request-to-response lifecycle.

### 2. `InventoryAlert.Jobs.Tests`
**Target:** The background queue processing and Hangfire scheduling loop.
- **Style:** Black-box. Injects messages directly into SQS (`inventory-events`).
- **Validation:** Monitoring `inventory-worker` logs, message deletion status, and DLQ (Dead Letter Queue) placement.
- **Dependencies:** Requires the `inventory-worker` container to be running.
- **Best for:** Verifying SQS routing, retries, DLQ movement, and Hangfire job execution side-effects.

### 3. `InventoryAlert.Handler.Tests` (The "Gold Standard")
**Target:** Specific service or handler implementations using production DI without the full network/queue overhead.
- **Style:** White-box. Resolves handlers directly from the production dependency injection graph.
- **Validation:** **In-memory logs** via `TestLoggerProvider`, WireMock call history, and direct Repository state assertions.
- **Dependencies:** Requires infrastructure services (Postgres, Redis, WireMock, Moto) but **not** the API or Worker containers.
- **Best for:** Complex business logic, edge cases, and verifying interaction with third-party APIs (Finnhub).

---

## 🗺️ High-Level Architecture

```text
 Api.Tests
 -> RestSharp Client
 -> running inventory-api container
 -> controllers + middleware + filters
 -> Postgres/Redis/SQS/WireMock
 -> assert: response status + payload + docker logs (via CorrelationId)

Jobs.Tests
 -> SQS Test Client (AWSSDK)
 -> running inventory-worker container
 -> SQS Poller + Router + Handlers
 -> Postgres/DynamoDB/Redis/WireMock
 -> assert: message deleted/DLQ + docker logs (via MessageId)

Handler.Tests
 -> Test Fixture (SetupDI)
 -> PRODUCTION DI Graph (InventoryAlert.Worker/Api)
 -> Resolve concrete handler/service from IServiceProvider
 -> Business Logic + Repository access
 -> assert: WireMock requests + in-memory log list + DB state
 ```

---

## 🛠️ Infrastructure Services

| Service | Tool | Purpose |
|---|---|---|
| **AWS Emulator** | Moto | Mock SQS and DynamoDB. Endpoint: `http://localhost:5000` |
| **HTTP Mock** | WireMock | Mock Finnhub API. Endpoint: `http://localhost:9091` |
| **Relational DB** | PostgreSQL | Real database for persistent state. Port: `5433` |
| **Cache** | Redis | Real Redis for SignalR backplane and caching. Port: `6379` |
| **Logs** | Seq | Structured log viewer for all test tiers. Port: `5341` |

---

## 🧪 Detailed Setup Flow

### Handler Test Setup (Recommended for Reuse)
The `HandlerTestFixture` is the cleanest pattern for business logic validation:
1. **Bootstrap Configuration**: Loads `appsettings.json` which redirects all external endpoints to WireMock/Moto.
2. **Setup DI**: Reuses the production `AddInfrastructure` and `AddApplicationServices` methods to ensure the test environment matches production.
3. **Capture Logs**: Registers a `TestLoggerProvider` to intercept `ILogger<T>` calls and store them in a searchable list.
4. **State Reset**: Before every test:
   - Truncates PostgreSQL tables (via Respawn).
   - Resets WireMock request history (`/__admin/reset`).
   - Clears the in-memory log list.

### API & Jobs Log Verification
To verify behavior in running containers, we use a **Docker Log Reader**:
- API tests filter logs by `X-Correlation-Id`.
- Jobs tests filter logs by `MessageId` (from the SQS envelope).
- This allows asserting that specific internal steps happened (e.g., "Successfully updated cost basis") even if they aren't visible in the HTTP response.

---

## 📈 Assertion Strategy Comparison

| Feature | Api.Tests | Jobs.Tests | Handler.Tests |
|---|---|---|---|
| **Speed** | Slow (container startup) | Medium (queue polling) | **Fast** (direct invocation) |
| **Determinism** | Moderate (log timing) | Moderate (polling waits) | **High** (in-memory) |
| **Realism** | Highest (full stack) | High (full worker) | Moderate (internal logic) |
| **Log Assertions** | Docker Scraping | Docker Scraping | **Direct List Search** |
| **External Calls** | WireMock Admin API | WireMock Admin API | **WireMock Admin API** |
