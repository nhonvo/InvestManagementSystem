# 🏗️ Integration Test Refactoring & Implementation Plan

## 1. Executive Summary & Vision
This plan outlines the strategic migration of the current `InventoryAlert.IntegrationTests` and `InventoryAlert.E2ETests` into a standardized, high-reliability testing framework based on the **Consumer Pattern**. We will move away from overlapping "black-box-only" tests toward a precision-engineered three-tier structure:
1.  **Handler Integration (White-box)**: Fast, DI-based tests for core logic.
2.  **API Integration (Refined Black-box)**: Focused on HTTP/Middleware/Logs.
3.  **Jobs Integration (Orchestration)**: Focused on SQS/Hangfire/DLQ semantics.

The primary goal is to reduce flakiness, improve debuggability through log assertions, and ensure the DI graph used in tests is 100% identical to production.

---

## 2. Solution Directory Reorganization

We will restructure the testing folder to accommodate the new projects:

```text
InventoryManagementSystem/
├── InventoryAlert.UnitTests/            # Stays as-is
├── InventoryAlert.IntegrationTests/     # REFACTORED: API Integration only
│   ├── Abstractions/
│   ├── Shared/
│   │   ├── Config/
│   │   │   ├── TestFixture.cs           # Docker Log Reader + HTTP Client setup
│   │   │   └── ActionTestConfig.cs      # Correlation-ID log filter logic
│   │   └── Helpers/
│   ├── Tests/
│   │   ├── Auth/
│   │   ├── Portfolio/
│   │   └── Stocks/
│   └── appsettings.json                 # Points to Docker Container URLs
├── InventoryAlert.HandlerTests/         # NEW: White-box Integration
│   ├── Infrastructure/
│   │   ├── SetupDI.cs                   # PRODUCTION DI Reuse logic
│   │   ├── HandlerTestFixture.cs        # In-memory Log + DB Reset logic
│   │   └── TestLoggerProvider.cs        # Capture ILogger<T> to List<LogEntry>
│   ├── Tests/
│   │   ├── Handlers/                    # SQS Handlers
│   │   └── Services/                    # Domain Services (from API)
│   ├── TestData/
│   │   ├── Requests/
│   │   ├── Seeds/
│   │   └── Expectations/
│   └── appsettings.json                 # Points to Localhost (WireMock/Moto)
└── InventoryAlert.JobsTests/            # NEW: Background Worker Integration
    ├── Shared/
    │   ├── Config/
    │   │   ├── JobTestFixture.cs        # SQS Client + Docker Log Reader
    │   │   └── TestBeforeAfter.cs       # Data Seeding logic
    ├── Tests/
    │   ├── Sqs/                         # Routing & DLQ logic
    │   └── Hangfire/                    # Recurring Job triggers
    └── appsettings.json
```

---

## 3. Phase 1: Foundation & Shared Infrastructure

### 3.1 Docker Compose Enhancements
Update `docker-compose.yml` to support the new testing patterns:
- Ensure `inventory-api` and `inventory-worker` have fixed container names for the `DockerLogReader`.
- Verify `moto` (AWS) and `wiremock` (Finnhub) are consistently reachable on fixed ports.
- Add a `healthcheck` to the `api` and `worker` containers so tests can wait for readiness.

### 3.2 The Test Logger Provider (HandlerTests)
Implementation of `TestLoggerProvider.cs` to capture logs in memory:
- Create a `LogEntry` record: `(LogLevel Level, string Message, Exception? Exception, string Category)`.
- Implement `ILogger` to append to a thread-safe `ConcurrentBag<LogEntry>`.
- Implement `ILoggerProvider` to return the test logger.
- Add helper methods: `AssertLogContains(string fragment)`, `GetLogsByCategory(string category)`.

### 3.3 The Docker Log Reader (Api/Jobs Tests)
A utility to scrape logs from running containers:
- Use `Docker.DotNet` library to stream container logs.
- Implement a `CaptureLogsAsync(string containerName, DateTime since)` method.
- Add a `LogFilter` that searches for `CorrelationId` (API) or `MessageId` (Worker).

---

## 4. Phase 2: Handler Integration Tests (White-box)

This project is the highest priority as it provides the fastest integration feedback.

### 4.1 Production DI Reuse (`SetupDI.cs`)
- Implement `SetupJobsInfrastructure` and `SetupApiInfrastructure`.
- Method signature: `public static IServiceProvider BuildServiceProvider(IConfiguration config)`.
- Must call `services.AddInfrastructure(settings)` and `services.AddApplicationServices()`.
- **Crucial**: Use `services.Replace()` to swap out `IFinnhubClient` with a real implementation that points to WireMock, and `ISqsService` to point to Moto.

### 4.2 The Handler Test Fixture
- Responsibility: Lifecycle of a single test.
- `ResetStateAsync()`:
  - Calls `Respawn` to clear Postgres.
  - Calls `WireMock.Reset()` to clear requests.
  - Clears `TestLoggerProvider` bag.
- `ResolveHandler<T>()`: Gets the service from the provider.

### 4.3 Mapping Scenarios to Files
- Create `TestData/Requests/TC123_PriceDrop_Alert.json`.
- Create `TestData/Expectations/TC123_ExpectedLogs.json`.
- The Base Class will automatically load these based on the `[Fact(DisplayName = "TC123")]` attribute.

---

## 5. Phase 3: API Integration Tests (Refined Black-box)

Refactor existing `InventoryAlert.IntegrationTests` to use the **Correlation-ID Log Pattern**.

### 5.1 ActionTestConfig Implementation
This helper will wrap every HTTP call:
```csharp
public async Task<TestResult<T>> RunActionAndViewLog<T>(Func<Task<RestResponse<T>>> action)
{
    var startTime = DateTime.UtcNow;
    var response = await action();
    var correlationId = response.Headers.FirstOrDefault(h => h.Name == "X-Correlation-Id")?.Value?.ToString();
    
    // Wait for logs to flush to Docker
    await Task.Delay(500);
    var logs = await _logReader.GetLogsByCorrelationId(correlationId, startTime);
    
    return new TestResult<T>(response, logs);
}
```

---

## 6. Phase 4: Jobs Integration Tests (Worker/SQS)

Focused on the `Worker` container's ability to process external triggers.

### 6.1 SQS Injection Flow
- Test sends a raw JSON to Moto SQS.
- Test polls the Worker logs for `MessageId`.
- **Scenario: DLQ Movement**:
  - Send a message with `EventType: "test.poison.v1"`.
  - Assert logs show "Critical failure processing message".
  - Assert message appears in `inventory-event-dlq` after 5 retries.

---

## 7. Phase 5: Implementation Roadmap

### 7.1 Week 1: Core Infrastructure
- [ ] Install `Docker.DotNet`, `Respawn`, `WireMock.Net`.
- [ ] Create `TestLoggerProvider.cs`.
- [ ] Create `DockerLogReader.cs`.
- [ ] Create `SetupDI.cs`.
- [ ] Implement `ActionTestConfig.cs`.

### 7.2 Week 2: Handler Tests Migration
- [ ] **MarketPriceAlertHandler**.
- [ ] **StockDataService**.
- [ ] **PortfolioService**.
- [ ] **AlertRuleService**.

### 7.3 Week 3: API Tests Refactoring
- [ ] **Auth Suite**.
- [ ] **Portfolio Suite**.
- [ ] **Stocks Suite**.
- [ ] **Notification Suite**.

### 7.4 Week 4: Jobs & SQS Suite
- [ ] **SQS Routing**.
- [ ] **Retry Logic**.
- [ ] **Cleanup Job**.
