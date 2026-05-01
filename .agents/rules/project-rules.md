---
trigger: always_on
glob:
description:
---

# Project Rules — InventoryAlert

---
description: Project-level coding standards for the InventoryAlert solution.
type: rules
status: active
version: 2.1
tags: [rules, coding-standards, ddd, csharp, inventoryalert]
last_updated: 2026-05-01
---

These rules apply to **every** code change in this repository. They are the project-specific layer on top of your global standards.

## Global Coding Standards

- Always follow **Clean Architecture** and **Clean Code** principles.
- Prefer readability over cleverness.

## Code Style

### Architecture (DDD boundaries)

- **Layer discipline is absolute.** Domain must have zero imports from Application, Infrastructure, or Web.
- **The master development flow is `/feature-flow`.** Always follow it top-to-bottom. Do not skip phases.
- **Before placing any file**, confirm the correct layer and namespace in `.agents/skills/ddd-architecture/SKILL.md`.

### C# coding standards

- **Primary constructors (C# 12)** are the preferred injection style across all layers.
- **No `async` without `await`** — remove the `async` keyword; return `Task.FromResult(...)` instead (fixes CS1998).
- **`CancellationToken ct`** must be the last parameter in every service and repository method.
- **`string.Empty`** over `""` for all empty string defaults on entity properties.
- **`null!` suppressor** is forbidden unless accompanied by a comment explaining why.
- Private fields: `_camelCase`. Everything else: `PascalCase`.

### Python scripts in `.agents/`

- Use type hints on all functions.
- Keep scripts small, deterministic, and readable (these are developer tools, not production code).

### EF Core / database

- Every **write** (Create, Update, Delete, Bulk) must run inside `_unitOfWork.ExecuteTransactionAsync`.
- Simple single-inserts with no dependencies may use `_unitOfWork.SaveChangesAsync` instead.
- All **read-only** EF queries use `.AsNoTracking()`.
- Never inject `AppDbContext` directly into Application-layer services.
- Never modify auto-generated `ModelSnapshot.cs` manually.
- Migration naming: `Verb + Entity + Detail` e.g. `AddNotificationDetails`.
- Run migrations from `InventoryManagementSystem/` with `--project InventoryAlert.Api`.

## Workflow

- Explore the codebase before implementing changes (BM25 search is preferred).
- Plan before coding on complex tasks.
- Run tests after making code changes (prefer the smallest relevant test selection first).

## Transaction Capture Pattern

```csharp
// ❌ BAD — blank entity returned if lambda throws
AlertRule updated = new();
await _unitOfWork.ExecuteTransactionAsync(async () => {
    updated = await _repo.UpdateAsync(entity);
}, ct);
return MapToResponse(updated); // may map a blank entity!

// ✅ GOOD — result assigned inside the lambda
AlertRuleResponse result = null!;
await _unitOfWork.ExecuteTransactionAsync(async () => {
    var updated = await _repo.UpdateAsync(entity);
    result = MapToResponse(updated);
}, ct);
return result;
```

## External API (Finnhub)

- Always null-check: `if (quote?.CurrentPrice is null or 0) skip`.
- Never throw on Finnhub failure — log and skip.
- Use `ILogger<T>`. `Console.WriteLine` is banned.

## Controller Rules

- Controllers are thin: call service → return `IActionResult`. No business logic.
- All `appsettings.*.json` with real credentials are gitignored. Only `appsettings.Example.json` is committed.

## Testing Standards

- No production code merged without: happy path + not-found + transaction call count verified.
- `ExecuteTransactionAsync` mocks **must** invoke the delegate:
  ```csharp
  .Returns<Func<Task>, CancellationToken>((action, _) => action());
  ```
- Repository integration tests use EF InMemory with `Guid.NewGuid().ToString()` as database name.
- No `Thread.Sleep` in tests.

## Git Conventions

- Write clear, concise commit messages.
- Never commit sensitive data (API keys, credentials).

### Commit message format

```
<type>(<scope>): <short description>

Types: feat | fix | test | refactor | chore | docs
Scope: domain | application | infrastructure | web | tests
```

Examples:
```
feat(application): add paging to WatchlistService
fix(infrastructure): remove async keyword from GenericRepository.UpdateAsync
test(application): add StockDataServiceTests for peers caching
refactor(web): move business logic from controller to service
docs(doc): sync unit_test_plan with implementation
```

## Communication

- Ask clarifying questions before architectural changes.
- Explain reasoning for non-obvious decisions.
