# 🏗️ Integration Test Refactoring & Implementation Plan (Unified Project)

## 1. Executive Summary & Vision
This plan outlines the strategic consolidation of all integration testing tiers into the existing **`InventoryAlert.IntegrationTests`** project. We will transform this project from a "black-box-only" suite into a comprehensive **Three-Tier** framework:
1.  **Tier 1: Handler Tests (White-box)**: Fast, DI-based tests for core logic (Services and SQS Handlers).
2.  **Tier 2: API Tests (Refined Black-box)**: Focused on HTTP/Middleware/Docker Log correlation.
3.  **Tier 3: Jobs & Orchestration Tests (Whole Flow)**: Verifies the "System Plumbing" (API -> SNS -> SQS -> Worker -> Side Effects).

The primary goal is to ensure **System-Wide Quality** by proving that all projects (Api, Infrastructure, Worker) communicate correctly through the event bus and produce the expected business outcomes.

---

## 2. System Orchestration Scenarios (The "Whole Flow")

The ultimate proof of application quality is the successful execution of cross-boundary scenarios. We will implement these "Whole Flow" tests in Tier 3:

| Scenario | Start Point | The "Flow" | Validation Point |
|---|---|---|---|
| **Price Alert Cycle** | `POST /api/v1/events` | API -> SQS -> `MarketPriceAlertHandler` -> Worker Logic | `GET /api/v1/notifications` |
| **Position Discovery** | `POST /api/v1/portfolio/positions` | API -> Finnhub Mock -> DB Insert -> SQS Alert | `GET /api/v1/stocks/{s}/quote` |
| **Holdings Evaluation** | `POST /api/v1/portfolio/{s}/trades` | API -> Trade Ledger -> SQS LowHoldings | `GET /api/v1/notifications` |
| **System Sync** | `POST /api/v1/stocks/sync` | API -> Hangfire Job -> Parallel Quotes -> Alerts | DB `PriceHistory` & User Feed |

---

## 3. Solution Directory Reorganization

We will restructure the `InventoryAlert.IntegrationTests` project internally:

```text
InventoryAlert.IntegrationTests/
├── Abstractions/
│   ├── BaseIntegrationTest.cs           # Base for Tier 2/3 (Container-based)
│   └── HandlerTestBase.cs               # Base for Tier 1 (DI-based)
├── Infrastructure/                      # Shared logic for all Tiers
│   ├── SetupDI.cs                       # PRODUCTION DI Reuse logic (for Tier 1)
│   ├── TestFixture.cs                   # Container management & Docker Log Reader
│   ├── TestLoggerProvider.cs            # In-memory Log capture (for Tier 1)
│   └── ActionTestConfig.cs              # Correlation-ID log filter logic
├── Tests/
│   ├── Handlers/                        # TIER 1: White-box SQS Handlers
│   ├── Services/                        # TIER 1: White-box Domain Services
│   ├── Api/                             # TIER 2: HTTP Endpoint tests (log-aware)
│   └── Jobs/                            # TIER 3: Background Worker Orchestration
├── TestData/                            # Scenario-based JSON files
│   ├── Requests/
│   ├── Seeds/
│   └── Expectations/
└── appsettings.test.json                # Dual-mode config (Localhost vs Docker)
```

---

## 3. Phase 1: Foundation & Shared Infrastructure

### 3.1 Project Enhancement
- Update `InventoryAlert.IntegrationTests.csproj` to include:
  - `Docker.DotNet` (for log scraping).
  - `WireMock.Net` (for Finnhub mocking).
  - `Respawn` (for fast DB cleanup).
  - `Moq` & `FluentAssertions`.

### 3.2 The Test Logger Provider (For Tier 1)
- Implement `TestLoggerProvider.cs` to capture `ILogger<T>` calls in memory.
- This allows Tier 1 tests to assert logic like `AssertLogContains("Alert triggered")` without container scraping.

### 3.3 The Docker Log Reader (For Tier 2 & 3)
- Implement a utility to stream and filter logs from the running `inventory-api` and `inventory-worker` containers.
- Filter by `X-Correlation-Id` (API) or `MessageId` (Worker).

### 3.4 WireMock Integration (External API Mocking)
The system uses a standalone WireMock container (`inventory_wiremock` on port `9091`) to simulate the Finnhub API.
- **Base URL**: `http://localhost:9091`
- **Mapping Directory**: `InventoryAlert.IntegrationTests/Wiremock/mappings/`
- **Configuration**:
  - For **Tier 1 (DI-based)**: `SetupDI.cs` must replace the `Finnhub:ApiBaseUrl` with the WireMock URL.
  - For **Tier 2/3 (Container-based)**: The `inventory-api` and `inventory-worker` containers are already configured via `docker-compose.yml` to point to `http://wiremock:8080`.

We will use two mapping strategies:
1.  **Static Mappings**: JSON files in the `/mappings` folder for common scenarios (e.g., AAPL always returns $150).
2.  **Dynamic Mappings**: Use `WireMock.Net` client in C# to create per-test stubs for edge cases (e.g., simulating a 429 Rate Limit error).

---

## 4. Phase 2: Tier 1 - Handler Integration Tests (White-box)

### 4.1 Production DI Reuse
- Create `SetupDI.cs` inside the integration project.
- Reuse `services.AddInfrastructure(settings)` and `services.AddApplicationServices()`.
- Use `services.Replace()` to redirect external service calls to WireMock (Finnhub) and Moto (SQS).

### 4.2 Handler Test Fixture
- Responsibility: Truncate PostgreSQL using `Respawn` and reset WireMock between tests.

---

## 5. Phase 3: Tier 2 - API Integration Tests (Log-Aware)

Refactor existing tests to use the `ActionTestConfig` pattern:
- Every HTTP call returns both the `RestResponse` AND the filtered Docker logs.
- Allows verifying internal state changes that don't appear in the HTTP body.

---

## 6. Phase 4: Tier 3 - Jobs & SQS Orchestration

- Focus on verifying the "Plumbing" (SNS -> SQS -> Worker).
- **Scenario: DLQ Movement**:
  - Inject poison message into Moto SQS.
  - Assert Worker logs show failure.
  - Assert message appears in `inventory-event-dlq`.

---

## 7. Implementation Roadmap

### 7.1 Week 1: Core Infrastructure
- [ ] Implement `TestLoggerProvider` and `DockerLogReader`.
- [ ] Implement `SetupDI` for production dependency reuse.
- [ ] Implement `ActionTestConfig` for log correlation.

### 7.2 Week 2: Tier 1 Migration (Services & Handlers)
- [ ] Migrate `MarketPriceAlertHandler` and `PortfolioService` tests to the new DI-based structure.
- [ ] Implement WireMock mappings for all Finnhub scenarios.

### 7.3 Week 3: Tier 2 Refactoring (API Controllers)
- [ ] Refactor existing Controller tests to include log-based verification.
- [ ] Verify symbol discovery flow (logs "DB miss -> Finnhub call").

### 7.4 Week 4: Tier 3 & CI Integration
- [ ] Implement SQS routing and DLQ tests.
- [ ] Update `.github/workflows/ci.yml` to execute the unified project with category filters.
