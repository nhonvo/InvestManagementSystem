# Integration Testing Framework: Implementation Guide

This guide provides a step-by-step blueprint for building a professional-grade integration testing framework for the **InventoryAlert.Api** and **InventoryAlert.Worker**.

---

## 🛠️ Step 1: Project & Dependency Setup

### 1.1 Project References
The `InventoryAlert.IntegrationTests` project must reference all core layers:
- `InventoryAlert.Api` (for `WebApplicationFactory`)
- `InventoryAlert.Worker` (for Job testing)
- `InventoryAlert.Infrastructure` (for direct DbContext/Repo access)

### 1.2 Package Requirements
Add these to `InventoryAlert.IntegrationTests.csproj` (versions managed in `Directory.Packages.props`):
- **Core**: `Microsoft.AspNetCore.Mvc.Testing`, `xunit`, `FluentAssertions`
- **Containers**: `Testcontainers.PostgreSql`, `Testcontainers.Redis`, `Testcontainers.LocalStack`
- **Utility**: `Respawn` (for fast DB cleanup)

---

## 🏗️ Step 2: Infrastructure Orchestration (Fixtures)

We need a central class to manage the lifecycle of Docker containers to avoid starting them for every single test.

### 2.1 The `SharedDatabaseFixture`
- **Responsibility**: Start Postgres container once per test session.
- **Logic**: 
    1. Initialize `PostgreSqlBuilder`.
    2. Run `container.StartAsync()`.
    3. Expose the `ConnectionString` to all tests.
    4. Handle `DisposeAsync()` to kill the container at the end.

---

## 🌐 Step 3: API Test Harness (WebApplicationFactory)

This is the bridge between your test code and the running API in memory.

### 3.1 `IntegrationTestFactory` Implementation
Create a class inheriting from `WebApplicationFactory<Program>`:
1. **ConfigureWebHost**:
   - **Remove Production DB**: Unregister `DbContextOptions<InventoryDbContext>`.
   - **Attach Test DB**: Register the DbContext using the `SharedDatabaseFixture` connection string.
   - **Mock External APIs**: Replace `IFinnhubClient` with a mock or a controlled stub.
   - **Auth Bypass**: Optionally add a `TestAuthHandler` to simulate authenticated headers without hitting a real identity provider.

---

## 👷 Step 4: Worker/Jobs Test Harness

Testing background jobs requires verifying both the **trigger** (Hangfire) and the **execution** (SQS logic).

### 4.1 Testing "The Publisher"
- Verify that when a product is created, a message is sent to the `LocalStack` SQS queue.
- Assert using `aws-sdk` inside the test to peek at the queue.

### 4.2 Testing "The Job"
- Instantiate the Job class (e.g., `PriceSyncJob`) directly via the Service Provider.
- Manually invoke `ExecuteAsync`.
- Assert side effects in the database (e.g., `PriceHistory` entries created).

---

## 🧹 Step 5: The "Base Class" Pattern

Every integration test should inherit from a `BaseIntegrationTest` to ensure a consistent clean state.

### 5.1 `BaseIntegrationTest` Logic
- **`Constructor`**: Resolves `IServiceProvider`, `HttpClient`, and `InventoryDbContext`.
- **`ResetState`**: Uses `Respawn` to truncate all tables (except `__EFMigrationsHistory`).
- **`ExecuteInScope`**: A helper to run logic within a fresh `IServiceScope` (crucial for verifying persistence).

---

## 🧪 Step 6: Example Test Structure

```csharp
public class ProductTests : BaseIntegrationTest
{
    [Fact]
    public async Task CreateProduct_ShouldPersistAndNotify()
    {
        // 1. Arrange
        var request = new CreateProductRequest("AAPL", "Apple Inc.");

        // 2. Act
        var response = await Client.PostAsJsonAsync("/api/v1/products", request);

        // 3. Assert (API Response)
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // 4. Assert (Database State)
        await ExecuteInScope(async (db, _) => {
            var product = await db.Products.FirstOrDefaultAsync(p => p.TickerSymbol == "AAPL");
            product.Should().NotBeNull();
        });

        // 5. Assert (Messaging)
        var messages = await SqsClient.ReceiveMessagesAsync("event-queue");
        messages.Should().Contain(m => m.Body.Contains("AAPL"));
    }
}
```

---

## 📂 Project Structure & Naming

To maintain a scalable test suite, we follow a strict directory and naming hierarchy.

### 4.1 Directory Mapping
```text
InventoryAlert.IntegrationTests/
├── Abstractions/             # Base classes (BaseIntegrationTest.cs)
├── Fixtures/                 # Infrastructure lifecycle (SharedDbFixture.cs)
├── Helpers/                  # Auth factories, Data generation (TestData.cs)
├── Tests/
│   ├── Api/                  # API endpoint tests (ProductApiTests.cs)
│   └── Worker/               # Background job tests (PriceSyncJobTests.cs)
└── appsettings.test.json     # Test-specific configuration
```

### 4.2 Naming Conventions
- **Files**: Must end with `Tests.cs` (e.g., `ProductLifecycleTests.cs`).
- **Methods**: Use the `[Action]_[ExpectedResult]_[Condition]` pattern.
  - *Example*: `CreateProduct_ShouldReturnCreated_WhenRequestIsValidAsync`
- **Scopes**: Use `ExecuteInScope` for any direct DB assertions to ensure transaction isolation.

---

## 💻 Essential Commands

| Action | Command |
| :--- | :--- |
| **Run All Tests** | `dotnet test` |
| **Run Specific Class** | `dotnet test --filter "FullyQualifiedName~ProductTests"` |
| **Run with Logging** | `dotnet test --logger "console;verbosity=detailed"` |
| **Clean Test Artifacts** | `dotnet clean` |

---

## 📚 Library Reference

| Library | Purpose |
| :--- | :--- |
| **Microsoft.AspNetCore.Mvc.Testing** | Provides the `WebApplicationFactory` to host the API in-memory. |
| **Testcontainers.PostgreSql** | Spins up a real, temporary Postgres instance in Docker. |
| **Respawn** | Intelligent database cleaner that resets data without dropping tables. |
| **FluentAssertions** | Expressive assertions like `.Should().BeEquivalentTo()`. |
| **Moq** | Used to stub external services like Finnhub or SMTP providers. |

---

## ✅ Step 7: Execution & CI/CD

1. **Local**: Run `dotnet test` (requires Docker Desktop/Engine).
2. **CI (GitHub Actions/ADO)**: 
   - Uses `service-containers` or simply rely on Testcontainers (it works in most CI environments with Docker).
   - Set environment variable `DOTNET_RUNNING_IN_CONTAINER=true`.

---

> [!IMPORTANT]
> **Data Seed Note**: Do not rely on production seed data. Every test should "Arrange" its own data or use a minimal `TestSeeder` to ensure predictable outcomes.
