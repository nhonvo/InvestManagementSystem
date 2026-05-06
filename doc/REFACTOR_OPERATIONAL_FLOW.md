# 🚀 Operational Flow: Unified Integration Test Refactor

This document defines the step-by-step operational flow for executing the refactoring within the **`InventoryAlert.IntegrationTests`** project.

---

## 🏁 Phase 0: Pre-Flight Checks
1.  **Infrastructure Up**: `docker compose up -d`
2.  **Baseline**: Ensure current `InventoryAlert.IntegrationTests` pass.
3.  **Branch Check**: Ensure you are on `refactor/integration-tests-three-tier`.

---

## 🏗️ Phase 1: Infrastructure Scaffolding
1.  **Project Update**: Add NuGet packages to `InventoryAlert.IntegrationTests.csproj`.
2.  **Internal Directory Setup**:
    - Create `Infrastructure/`, `Abstractions/`, and `Tests/` (sub-folders: `Handlers`, `Services`, `Api`, `Jobs`).
3.  **DI & Interceptors**:
    - Implement `Infrastructure/SetupDI.cs`.
    - Implement `Infrastructure/TestLoggerProvider.cs`.
    - *Verification (WireMock)*: Add a static mapping in `Wiremock/mappings/ping.json`. Call it via `HttpClient` from a test. If it returns 200 OK, the WireMock container and volume mapping are functional.
    - *Verification (DI)*: A "Smoke Test" resolves a service and captures a log entry.

---

## 💉 Phase 2: Tier 1 - White-box Implementation
1.  **Handler Selection**: Pick `MarketPriceAlertHandler`.
2.  **Mocking Phase**:
    - Define WireMock mapping for Finnhub quote.
    - Resolve handler from DI.
    - *Verification*: Call handler directly and assert that the `TestLoggerProvider` captured the "Processing alert" message.
3.  **DB Isolation**:
    - Run test multiple times.
    - *Verification*: Use `Respawn` in the fixture to ensure DB state is cleared.

---

## 📡 Phase 3: Tier 2 - API Log Correlation
1.  **Log Reader**:
    - Implement `Infrastructure/DockerLogReader.cs` using `Docker.DotNet`.
2.  **Correlation Test**:
    - Use `ActionTestConfig` to call a Controller.
    - *Verification*: Assert that logs retrieved from the `inventory-api` container contain the `X-Correlation-Id` from the response.

---

## 🔁 Phase 4: Whole-Flow Orchestration (The Quality Peak)
Focus: Verifying the complete system lifecycle (API -> Infra -> Worker -> Result).

1.  **Orchestration Scenario**:
    - Call `POST /api/v1/events` (Price Alert) via the API client.
2.  **Wait for Side-Effect**:
    - Use `WaitHelper.WaitForConditionAsync` to poll `GET /api/v1/notifications`.
3.  **Verification**: 
    - Assert that the notification created by the **Worker** appears in the **API** response for the user.
    - *Quality Proof*: This proves that the API, SQS, Worker Handlers, and Database are all correctly integrated.

---

## 🏁 Phase 5: Final Validation
1.  **Category Cleanup**: Ensure all tests have a `[Trait("Category", "TierN")]` attribute.
2.  **CI Execution**:
    ```powershell
    dotnet test --filter "Category=Tier1"
    dotnet test --filter "Category=Tier2"
    ```
3.  **Performance**: Verify that Tier 1 tests run in milliseconds compared to the seconds/minutes required for Tier 2/3.
