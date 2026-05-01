# 🛡️ Test Structure & Application Quality

Quality in InventoryAlert is enforced through a comprehensive, multi-layered approach that spans from static architecture analysis to full end-to-end (E2E) runtime validation. This document outlines the structural layout of our test projects and how every part of the application's quality is guaranteed.

---

## 🏗️ Test Solution Structure

The solution is divided into four distinct testing projects, each targeting a specific layer of the Testing Pyramid.

### 1. `InventoryAlert.UnitTests` (Fast & Isolated)
**Goal:** Verify pure business logic, calculations, and state changes without external dependencies.
- **Structure:**
  - `/Application` - Tests for Services (e.g., `StockDataServiceTests`, `PortfolioServiceTests`). Mocks `IUnitOfWork` and Repositories.
  - `/Web` - Tests for API Controllers. Verifies routing, HTTP status codes, and response mapping.
  - `/Worker` - Tests for internal worker logic (e.g., alert condition math).
  - `/Infrastructure` - Tests for isolated utility classes.
- **Quality Covered:** Handles edge cases, null inputs, math accuracy (e.g., `PercentDropFromCost`), and domain validation rules. Fast execution ensures immediate developer feedback.

### 2. `InventoryAlert.IntegrationTests` (Database & Infrastructure)
**Goal:** Verify that the application communicates correctly with its persistence and messaging layers.
- **Structure:**
  - `/Fixtures` - Docker setup and EF Core InMemory/Testcontainers initialization.
  - `/Tests/Api` - API tests that hit a real database but bypass the network using `WebApplicationFactory`.
  - `/Tests/Worker` - SQS message consumption, DLQ routing, and Hangfire job integration.
  - `/Wiremock` - Mocks for external Finnhub API calls to ensure resilience against third-party downtime.
- **Quality Covered:** Validates EF Core LINQ queries, database constraints, SQS serialization/deserialization, and transaction rollbacks.

### 3. `InventoryAlert.E2ETests` (Full System Flow)
**Goal:** Verify critical user journeys against a fully deployed environment (`docker-compose up`).
- **Structure:** Flat layout grouped by domain (e.g., `AuthE2ETests.cs`, `PortfolioE2ETests.cs`, `SqsRetryE2ETests.cs`).
- **Quality Covered:** Black-box testing. Proves that an API consumer can successfully authenticate, add a position, trigger a background Finnhub sync, and receive a SignalR notification via the Redis backplane.

### 4. `InventoryAlert.ArchitectureTests` (Static Enforcement)
**Goal:** Prevent architectural drift and tech debt.
- **Structure:** Uses `NetArchTest.Rules` to enforce Clean Architecture invariants automatically during the build.
- **Quality Covered:** 
  - *Rule:* `Domain` must not reference `Infrastructure` or `Api`.
  - *Rule:* `Infrastructure` must not reference `Api`.
  - *Rule:* All Controllers must have `[Authorize]` by default.

---

## 🛡️ Comprehensive Quality Coverage Matrix

How we guarantee quality across different operational domains:

| Domain/Component | How Quality is Ensured |
|---|---|
| **Database & Schema** | Integration tests use a transient database to verify EF Core Migrations and constraint violations (e.g., unique ticker symbols). |
| **Third-Party APIs (Finnhub)** | HTTP client resilience (Polly retries) is tested via Wiremock in Integration tests; API rate limits are enforced via Redis caching (`quote:{symbol}`). |
| **Asynchronous Workers** | E2E tests inject messages into Moto (SQS emulator) and poll the API to verify background job side-effects (e.g., `NewsSyncJob` populating DynamoDB). |
| **Concurrency & Race Conditions** | Unit tests verify `ExecuteTransactionAsync` isolation. E2E tests run concurrently to ensure EF Core handles concurrent inserts safely. |
| **Authentication & Security** | E2E tests ensure `401 Unauthorized` is returned for missing tokens and `403 Forbidden` for cross-user data access attempts. |
| **Front-End UI (Next.js)** | Strict TypeScript compiler checks, ESLint, and React Query caching ensure a robust, type-safe user experience. |

---

## 📈 Measuring Quality: Code Coverage

We use **Coverlet** and **ReportGenerator** to measure and visualize code coverage across our unit tests. This ensures that our critical business logic and finance math are thoroughly exercised.

### Automated Coverage Script
A convenience script, `code-coverage.bat`, is provided at the repository root to automate the entire process:
1.  **Ensures Tools**: Installs `dotnet-reportgenerator-globaltool` and `coverlet.console` globally if missing.
2.  **Runs Tests**: Executes `dotnet test` on the Unit Tests project with the `XPlat Code Coverage` collector enabled.
3.  **Generates Report**: Merges the Cobertura XML files into a human-readable HTML dashboard.
4.  **Opens Results**: Automatically opens the generated report in your default browser.

### How to Run
```powershell
# From the repository root
./code-coverage.bat
```

### Understanding the Results
The script generates a `/coverage/html/` directory. Open `index.html` to view:
- **Line Coverage**: The percentage of code lines executed by tests.
- **Branch Coverage**: Ensuring both paths of an `if` statement are tested.
- **Complexity Metrics**: Identifying methods that are too complex and likely need refactoring.

> 💡 **Tip**: We aim for **80%+ coverage** in the `Domain` and `Application/Services` layers. Coverage in the `Infrastructure` layer is secondary to integration and E2E tests.

---

## ⚙️ Automated Quality Gates (CI/CD)

Quality is strictly enforced automatically on every Pull Request via GitHub Actions (`.github/workflows/ci.yml`):

1. **Build & Lint:** Ensures C# 12 and Next.js strict mode compile without warnings.
2. **Architecture Check:** Runs `InventoryAlert.ArchitectureTests` to instantly block PRs that break Clean Architecture layer isolation.
3. **Unit & Integration Coverage:** Executes `dotnet test` with code coverage tracking to ensure core logic remains tested (`coverage.cobertura.xml`).
4. **E2E Docker Verification:** Spins up the full `docker-compose.yml` stack (API, Worker, Postgres, Redis, Moto) and runs the `E2ETests` suite to prove the system works dynamically as a whole before merge.

---

## 🩺 Runtime Quality (Observability)

Quality doesn't stop at deployment. The application guarantees runtime reliability through:

- **Structured Logging (Seq):** Every API request and background job logs to Seq (`http://localhost:5341`) with a `CorrelationId`. This allows tracing a single user action from the API, through SQS, down to the Worker execution.
- **Health Checks (`/health`):** Active HTTP probes for PostgreSQL, Redis, and SQS connectivity ensure that load balancers know the exact health state of both the API and Worker containers at all times.
- **Dead Letter Queues (DLQ):** SQS is configured with a `maxReceiveCount` of 5. Poison messages (e.g., bad Finnhub payloads) are isolated to `inventory-event-dlq` to prevent worker crash loops, ensuring high availability for healthy traffic.