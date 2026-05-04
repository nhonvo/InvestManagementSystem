# 🛡️ Test Structure & Application Quality

Quality in InventoryAlert is enforced through a comprehensive, multi-layered approach. We have consolidated our testing infrastructure into a single, high-performance integration project that covers everything from static architecture analysis to full system orchestration.

---

## 🏗️ Test Solution Structure

The solution is divided into three core testing projects:

### 1. `InventoryAlert.UnitTests` (Isolated Logic)
**Goal:** Verify pure business logic, calculations, and state changes with 100% mocked dependencies.
- **Scope:** Handles edge cases, null inputs, and finance math accuracy (e.g., `PercentDropFromCost`).
- **Speed:** Instant feedback (< 2 seconds for full suite).

### 2. `InventoryAlert.IntegrationTests` (Unified System Tests)
**Goal:** Verify that the application communicates correctly with its persistence (Postgres/Redis) and messaging (SQS/SNS) layers.
- **Architecture:** Organized into **Three Logical Tiers** (see [Integration Testing Tiers](./integration-testing-tiers.md)):
    - **Tier 1 (White-box)**: Direct logic testing using production DI.
    - **Tier 2 (Black-box)**: API surface testing with log-correlation.
    - **Tier 3 (Orchestration)**: End-to-end "Whole Flow" validation.
- **Isolation:** Uses **Respawn** for DB reset and **WireMock** for external API simulation.

### 3. `InventoryAlert.ArchitectureTests` (Static Enforcement)
**Goal:** Prevent architectural drift and tech debt using `NetArchTest`.
- **Enforcement:** Ensures `Domain` never references `Infrastructure` and all controllers are secured with `[Authorize]`.

---

## 🛡️ Comprehensive Quality Coverage Matrix

How we guarantee quality across different operational domains:

| Domain/Component | How Quality is Ensured |
|---|---|
| **Business Logic** | Tier 1 Integration tests resolve real services from the production DI graph to verify logic against a real DB. |
| **Database Integrity** | **Respawn** ensures every test starts with a clean DB. Tier 1/2 tests verify EF Core constraints and complex LINQ queries. |
| **External APIs** | **WireMock** simulates Finnhub. We test success paths, 429 rate limits, and 500 server errors without hitting real endpoints. |
| **System Orchestration** | Tier 3 tests verify the **Whole Flow**: API Call → SQS → Worker Job → DB Update → API Notification Feed. |
| **Observability** | **DockerLogReader** allows tests to "scrape" container logs, proving that internal request traces (CorrelationId) are correctly logged. |

---

## 📈 Measuring Quality: Code Coverage

We use **Coverlet** and **ReportGenerator** to visualize coverage.
- **Script:** Run `./code-coverage.bat` from the root.
- **Target:** **80%+ coverage** in the `Domain` and `Application` layers.

---

## ⚙️ Automated Quality Gates (CI/CD)

Every Pull Request is blocked until the following passes:
1. **Build & Lint:** Next.js and .NET 10 compilation.
2. **Architecture Check:** Runs `NetArchTest` rules.
3. **Unit Tests:** Executes isolated logic tests.
4. **Integration Suite:** Spins up the full Docker stack and executes all 3 Tiers of integration tests to prove "Whole-Flow" stability.

---

## 🩺 Runtime Quality (Observability)

- **Structured Logging (Seq):** Trace single user actions across projects via `CorrelationId`.
- **Health Checks (`/health`):** Active probes for Postgres, Redis, and SQS connectivity.
- **DLQ isolation:** Poison messages are moved to `inventory-event-dlq` to prevent worker crash loops.
