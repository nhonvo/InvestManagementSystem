# Project Rules — InventoryAlert.Api

These rules apply to **every** code change. They are non-negotiable defaults.
They complement (and may narrow) the global rules in `user_global`.

---

## 1. Architecture Rules

- **Layer discipline is absolute.** Domain has zero imports from Application, Infrastructure, or Web. Violating this breaks the DDD contract.
- **The master development flow is `/feature-flow`.** Always follow it top-to-bottom. Do not skip phases.
- **Before placing any file**, read the DDD skill (`ddd-architecture`) to confirm the correct layer and namespace.

## 2. C# Coding Standards

- **Primary constructors** (C# 12) are the preferred injection style:
  ```csharp
  public class ProductService(
      IUnitOfWork unitOfWork,
      IProductRepository productRepository) : IProductService
  { ... }
  ```
- **No `async` without `await`.** Methods with no async operation must not use the `async` keyword (CS1998). Remove `async` and return the value directly or wrap in `Task.FromResult`.
- **`CancellationToken ct`** must be the last parameter in every service and repository method.
- **`string.Empty`** over `""` for all empty string defaults on entity properties.
- **`null!`** suppressor is forbidden unless documented with a comment explaining why.
- Private fields: `_camelCase`. Everything else: `PascalCase`.

## 3. EF Core / Database Rules

- Every **write** (Create, Update, Delete, Bulk) must run inside `_unitOfWork.ExecuteTransactionAsync`.
- Simple single-inserts with no dependencies may use `_unitOfWork.SaveChangesAsync` instead.
- All **read-only** EF queries use `.AsNoTracking()`.
- Never inject `AppDbContext` directly into Application-layer services.
- Never modify auto-generated `ModelSnapshot.cs` manually.
- Migration names follow: `Verb + Entity + Detail`. Example: `AddStockAlertThresholdToProduct`.

## 4. External API Rules (Finnhub)

- Always null-check Finnhub responses: `if (quote?.CurrentPrice is null or 0) skip`.
- Never throw on Finnhub failure — log and skip the product.
- Use `ILogger<T>` for all errors. `Console.WriteLine` is banned.

## 5. API / Controller Rules

- Controllers are thin. The maximum allowed logic in a controller action is:
  1. Call the service
  2. Return the correct `IActionResult` (`Ok`, `NotFound`, `Created`, `NoContent`)
- `UpdateStockCount` in `ProductsController` is a known SRP violation (tech debt). Do not replicate the same pattern in new controllers.
- All `appsettings.*.json` files with real credentials are **gitignored**. Only `appsettings.Example.json` is committed.

## 6. Testing Rules

- No production code is merged without at minimum:
  - Happy path test
  - Not-found / null edge-case test
  - Transaction execution verified (`Times.Once`)
- `ExecuteTransactionAsync` mocks **must** invoke the delegate. Bare `Returns(Task.CompletedTask)` is wrong.
- Repository integration tests use EF InMemory with `Guid.NewGuid().ToString()` as database name.
- No `Thread.Sleep` in tests. Use `async/await`.

## 7. Git Commit Convention

```
<type>(<scope>): <short description>

Types: feat | fix | test | refactor | chore | docs
Scope: domain | application | infrastructure | web | tests
```

Examples:
```
feat(application): add BulkInsert to ProductService
fix(infrastructure): remove async keyword from GenericRepository.UpdateAsync
test(application): add ProductServiceTests for SyncCurrentPricesAsync
refactor(web): move stock-count update logic from controller to service
```
