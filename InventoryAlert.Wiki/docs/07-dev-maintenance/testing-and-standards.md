# Testing & Coding Standards

## Testing Stack

- **xUnit** — Test runner
- **Moq** — Mocking framework
- **FluentAssertions** — Human-readable assertions
- **RestSharp** — HTTP client for E2E tests
- **EF Core InMemory** — Integration test database

## Test Projects

| Project | Purpose |
|---|---|
| `InventoryAlert.UnitTests` | Services, repositories, and domain logic (mocked dependencies) |
| `InventoryAlert.IntegrationTests` | Repository-level tests using EF Core InMemory |
| `InventoryAlert.E2ETests` | Full HTTP roundtrip tests against running Docker stack |
| `InventoryAlert.ArchitectureTests` | Layer dependency enforcement (NetArchTest) |

## Test Naming Convention

```
MethodName_StateUnderTest_ExpectedBehavior
// Examples:
CreateAlert_ValidInput_ReturnsCreatedAlert
GetQuote_FinnhubReturnsZero_ReturnsNull
SyncPrices_RuleBreached_WritesNotification
OpenPosition_SymbolNotInCatalog_ThrowsInvalidOperation
```

---

## Key Testing Patterns

### Mocking `IUnitOfWork.ExecuteTransactionAsync`

The delegate **must** be invoked in the mock, otherwise nothing inside the lambda runs:

```csharp
_unitOfWorkMock
    .Setup(u => u.ExecuteTransactionAsync(
        It.IsAny<Func<Task>>(),
        It.IsAny<CancellationToken>()))
    .Returns<Func<Task>, CancellationToken>((action, _) => action());
```

### Verify Trade Ledger Calls

```csharp
_tradeRepoMock.Verify(r => r.AddAsync(It.Is<Trade>(t =>
    t.TickerSymbol == "AAPL" &&
    t.Quantity == 10 &&
    t.Type == TradeType.Buy &&
    t.UserId == expectedUserId),
    It.IsAny<CancellationToken>()),
    Times.Once);
```

### E2E Test Base Setup

```csharp
// BaseE2ETest.cs — all E2E tests inherit this
protected async Task EnsureAuthenticatedAsync()
{
    var request = new RestRequest("api/auth/login", Method.Post);
    request.AddJsonBody(new LoginRequest("admin", "password"));
    var response = await Client.ExecuteAsync<AuthResponse>(request);
    JwtToken = response.Data!.AccessToken;
}
```

### Event Flow Testing (CQRS)
For endpoints that trigger background processes (like `POST /api/v1/events`), E2E testing ensures:
1. The API responds correctly with `202 Accepted`.
2. The infrastructure successfully routed the payload. E2E tests can verify side-effects by waiting a small delay and verifying the final state via a GET request to the read-store.

> ⚠️ **E2E tests require the Docker stack to be running** (`docker compose up --build`).

### Integration Test Database Naming

```csharp
// Use unique DB name per test class to prevent state bleed
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString())
    .Options;
```

---

## C# Coding Standards

| Rule | Description |
|---|---|
| **Primary Constructors** | Use C# 12 primary constructors across all layers |
| **`CancellationToken ct`** | Must be the last parameter in every async method |
| **No `async` without `await`** | Remove `async` keyword; return `Task.FromResult(...)` instead (fixes CS1998) |
| **`string.Empty`** | Use over `""` for empty string defaults on entity properties |
| **`null!` suppressor** | Forbidden unless accompanied by a comment explaining why |
| **Private fields** | `_camelCase`. Everything else: `PascalCase` |
| **`ILogger<T>`** | Use for all logging. `Console.WriteLine` is banned |
| **`AsNoTracking()`** | Required on all read-only EF Core queries |
| **FluentValidation** | Applied at Web layer only; controllers must not contain inline `if` validation |

## Transaction Capture Pattern

```csharp
// ❌ BAD — blank entity returned if lambda throws
StockListing updated = new();
await _unitOfWork.ExecuteTransactionAsync(async () => {
    updated = await _repo.UpdateAsync(entity);
}, ct);
return MapToResponse(updated); // may map a blank StockListing!

// ✅ GOOD — result assigned inside the lambda
PortfolioPositionResponse result = null!; // null-forgiving acceptable here: assigned before read
await _unitOfWork.ExecuteTransactionAsync(async () => {
    var updated = await _repo.UpdateAsync(entity);
    result = MapToResponse(updated);
}, ct);
return result;
```

## Git Commit Convention

```
<type>(<scope>): <short description>

Types: feat | fix | test | refactor | chore | docs
Scope: domain | application | infrastructure | web | tests
```

Examples:
```
feat(application): add BulkImport to PortfolioService
fix(infrastructure): remove async keyword from GenericRepository.UpdateAsync
test(application): add E2E coverage for AlertRulesController
refactor(web): extract trade recording to PortfolioService
```
