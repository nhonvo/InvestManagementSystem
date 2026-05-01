# 🚀 Operational Flow: Integration Test Refactor

This document defines the step-by-step operational flow for executing, verifying, and testing the integration test refactoring. It serves as the "Runbook" for developers or agents implementing the `integration-test-refactor-plan.md`.

---

## 🏁 Phase 0: Pre-Flight Checks
Before modifying code, ensure the current environment is stable.

1.  **Infrastructure Up**: Run `docker compose up -d` and wait for all containers to be `healthy`.
2.  **Baseline Test Run**: Run the existing integration tests:
    - `dotnet test InventoryAlert.IntegrationTests`
    - `dotnet test InventoryAlert.E2ETests`
    - *Success Criteria*: All existing tests pass. Capture the execution time for future comparison.
3.  **Environment Sync**: Ensure `appsettings.Example.json` matches your local `.env`.

---

## 🏗️ Phase 1: Building the Infrastructure (Week 1)
Focus: Scaffolding the new test projects and shared utilities.

1.  **Project Creation**:
    - Create `InventoryAlert.HandlerTests` and `InventoryAlert.JobsTests`.
    - Install NuGet packages: `FluentAssertions`, `Moq`, `xunit`, `Docker.DotNet`, `Respawn`, `WireMock.Net`.
2.  **DI Bootstrap**:
    - Implement `SetupDI.cs`. 
    - *Verification*: Write a "Smoke Test" that resolves `IUnitOfWork` and `IStockDataService`. If it doesn't throw, DI is wired correctly.
3.  **Log Interceptors**:
    - Implement `TestLoggerProvider`.
    - *Verification*: Write a test that logs a "Hello World" message and asserts that the `ConcurrentBag` contains it.

---

## 💉 Phase 2: Handler Test Execution (Week 2)
Focus: Migrating the core logic from Unit tests to high-fidelity Handler tests.

1.  **Test-the-Test (Red Phase)**:
    - Write a `MarketPriceAlertHandler` test but point it to a non-existent WireMock mapping.
    - *Verification*: The test **must fail** with a "No matching mapping found" error. This proves WireMock is actually in the loop.
2.  **Implementation (Green Phase)**:
    - Add the WireMock mapping for Finnhub.
    - Run the test.
    - *Verification*: Test passes. Check Seq (`localhost:5341`) to see the logs emitted during the test run.
3.  **State Isolation Check**:
    - Run the same test twice in a row.
    - *Verification*: Both must pass independently. If the second one fails due to duplicate keys, `Respawn` or `ResetAsync()` is not working.

---

## 📡 Phase 3: API Log-Correlation Flow (Week 3)
Focus: Updating the API tests to verify internal request traces.

1.  **Correlation Injection**:
    - Use `ActionTestConfig` to call `/api/v1/auth/login`.
    - *Verification*: Assert that the `RestResponse` contains the `X-Correlation-Id` header.
2.  **Docker Log Scraping**:
    - Call an endpoint that triggers a known log (e.g., `POST /positions` triggering symbol discovery).
    - *Verification*: Assert that `logs.Any(l => l.Contains("Symbol discovery started"))` is true.
    - *Fail-safe*: If logs are empty, increase the `Task.Delay` in `ActionTestConfig` to allow Docker buffers to flush.

---

## 🔁 Phase 4: Jobs & DLQ Verification (Week 4)
Focus: Verifying the "Unreachable" failure paths.

1.  **Poison Message Test**:
    - Inject a message with an invalid JSON body into SQS via Moto.
    - *Verification*: Wait for the worker logs to show "Critical failure".
2.  **DLQ Finality**:
    - Check the `inventory-event-dlq` count.
    - *Verification*: Count must be 1. This proves the SQS redrive policy is correctly configured and the Worker isn't swallowing errors.

---

## 🏁 Phase 5: Final Validation & Cleanup
Once all phases are complete, perform the final "Quality Gate".

1.  **Full Suite Execution**: Run all 4 test projects (Unit, Handler, Api, Jobs).
2.  **Coverage Audit**: Run `./code-coverage.bat`.
    - *Success Criteria*: Coverage on `Domain` and `Services` should have increased or stayed stable, even with logic moved from Unit to Handler tests.
3.  **Architecture Verification**: Run `InventoryAlert.ArchitectureTests`.
    - *Success Criteria*: No new layer violations introduced by the test projects.
4.  **Performance Check**: Compare the new suite execution time against the baseline from Phase 0.
    - *Success Criteria*: Handler tests should be significantly faster than the old E2E tests for the same logic.
