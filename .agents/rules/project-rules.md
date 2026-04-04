# Project Rules — InventoryAlert.Api
---
description: Project-level coding standards for InventoryAlert.Api
type: rules
status: active
version: 2.0
tags: [rules, coding-standards, ddd, csharp, inventoryalert]
last_updated: 2026-04-04
---

These rules apply to **every** code change in this project. They are non-negotiable defaults that complement the global `user_global` rules.

---

## 1. Architecture (DDD)

- **Layer discipline is absolute.** Domain must have zero imports from Application, Infrastructure, or Web.
- **The master development flow is `/feature-flow`.** Always follow it top-to-bottom. Do not skip phases.
- **Before placing any file**, confirm the correct layer and namespace in `.agents/skills/ddd-architecture/SKILL.md`.

---

## 2. C# Coding Standards

- **Primary constructors (C# 12)** are the preferred injection style across all layers.
- **No `async` without `await`** — remove the `async` keyword; return `Task.FromResult(...)` instead (fixes CS1998).
- **`CancellationToken ct`** must be the last parameter in every service and repository method.
- **`string.Empty`** over `""` for all empty string defaults on entity properties.
- **`null!` suppressor** is forbidden unless accompanied by a comment explaining why.
- Private fields: `_camelCase`. Everything else: `PascalCase`.

---

## 3. EF Core / Database

- Every **write** (Create, Update, Delete, Bulk) must run inside `_unitOfWork.ExecuteTransactionAsync`.
- Simple single-inserts with no dependencies may use `_unitOfWork.SaveChangesAsync` instead.
- All **read-only** EF queries use `.AsNoTracking()`.
- Never inject `AppDbContext` directly into Application-layer services.
- Never modify auto-generated `ModelSnapshot.cs` manually.
- Migration naming: `Verb + Entity + Detail` e.g. `AddStockAlertThresholdToProduct`.
- Run migrations from `InventoryManagementSystem/` with `--project InventoryAlert.Api`.

---

## 4. Transaction Capture Pattern

```csharp
// ❌ BAD — blank entity returned if lambda throws
Product updated = new();
await _unitOfWork.ExecuteTransactionAsync(async () => {
    updated = await _repo.UpdateAsync(entity);
}, ct);
return MapToResponse(updated); // may map a blank Product!

// ✅ GOOD — result assigned inside the lambda
ProductResponse result = null!;
await _unitOfWork.ExecuteTransactionAsync(async () => {
    var updated = await _repo.UpdateAsync(entity);
    result = MapToResponse(updated);
}, ct);
return result;
```

---

## 5. External API (Finnhub)

- Always null-check: `if (quote?.CurrentPrice is null or 0) skip`.
- Never throw on Finnhub failure — log and skip.
- Use `ILogger<T>`. `Console.WriteLine` is banned.

---

## 6. Controller Rules

- Controllers are thin: call service → return `IActionResult`. No business logic.
- `UpdateStockCount` in `ProductsController` is a known SRP violation (tracked tech debt). Do not replicate in new controllers.
- All `appsettings.*.json` with real credentials are gitignored. Only `appsettings.Example.json` is committed.

---

## 7. Testing Standards

- No production code merged without: happy path + not-found + transaction call count verified.
- `ExecuteTransactionAsync` mocks **must** invoke the delegate:
  ```csharp
  .Returns<Func<Task>, CancellationToken>((action, _) => action());
  ```
- Repository integration tests use EF InMemory with `Guid.NewGuid().ToString()` as database name.
- No `Thread.Sleep` in tests.

---

## 8. Git Commit Convention

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
docs(doc): sync unit_test_plan with implementation
```
