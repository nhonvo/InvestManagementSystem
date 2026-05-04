# 🧪 Integration Testing Tiers

InventoryAlert uses a **Three-Tier** architecture within the `InventoryAlert.IntegrationTests` project to balance testing speed with system realism.

---

## 🏗️ Technical Foundation

All tiers share a unified infrastructure:
- **`TestFixture`**: Manages the lifecycle of Postgres, WireMock, and the Docker Log Reader.
- **`SetupDI.cs`**: Reuses production code (Composition Root) to build the test dependency graph.
- **`Respawn`**: Automatically truncates the database between every test run.
- **`Sequential Execution`**: Enforced via `[Collection("IntegrationTests")]` to prevent data collisions.

---

## 🥈 Tier 1: White-box Handler Tests
**Goal:** Verify complex business logic using the real database but mocked external services.

- **How it works:** Resolves concrete services (e.g., `PortfolioService`) or SQS handlers (e.g., `MarketPriceAlertHandler`) directly from the test `IServiceProvider`.
- **Validation:** 
    - Direct database assertions using `IUnitOfWork`.
    - In-memory log assertions using `TestLoggerProvider` (avoids container scraping).
- **Tooling:** Production DI graph + Moq for third-party mocks.
- **Best For:** Price math, rule evaluation logic, and trade ledger updates.

---

## 🥉 Tier 2: Log-Aware API Tests
**Goal:** Verify the HTTP surface, middleware, and request-response lifecycle.

- **How it works:** Sends real HTTP requests to the running `inventory-api` container via `RestClient`.
- **Validation:** 
    - HTTP Status Codes and JSON payloads.
    - **Log Correlation**: Scrapes container logs for the specific `X-Correlation-Id` returned in the response. Proves that internal "invisible" side-effects occurred.
- **Tooling:** `RestSharp` + `DockerLogReader`.
- **Best For:** Authentication, route parameters, and status code correctness.

---

## 🥇 Tier 3: Whole-Flow Orchestration
**Goal:** Prove absolute application quality by verifying cross-project orchestration.

- **How it works:** Initiates an action via the API and waits for a side-effect in a different part of the system after background processing.
- **Scenario Example**:
    1. `POST /api/v1/events` (via API).
    2. Event travels through **SQS**.
    3. **Worker** consumes event and processes business logic.
    4. **Worker** updates the database.
    5. Test polls `GET /api/v1/notifications` (via API) to confirm the user sees the result.
- **Tooling:** `WaitHelper` for polling + `WireMock` Admin API for seeding external quotes.
- **Best For:** Proving that the API and Worker projects are correctly integrated via the event bus.

---

## 🛠️ Developer Patterns

### 1. Mocking External Services (WireMock)
Use the `WireMock` instance in the fixture to define behavior for Finnhub:

```csharp
Fixture.WireMock
    .Given(Request.Create().WithPath("/quote").WithParam("symbol", "AAPL"))
    .RespondWith(Response.Create().WithStatusCode(200).WithBody("{\"c\": 150}"));
```

### 2. Asserting Internal Logs (Tier 1)
```csharp
AssertLog("Dispatched real-time notification");
```

### 3. Waiting for Async Results (Tier 3)
```csharp
await WaitHelper.WaitForConditionAsync(async () => {
    var res = await Client.GetAsync("/api/v1/notifications");
    return res.Content.Contains("AAPL Alert");
}, timeoutSeconds: 30);
```
