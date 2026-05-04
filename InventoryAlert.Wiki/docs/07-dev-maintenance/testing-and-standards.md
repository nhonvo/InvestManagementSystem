# Coding Standards & Patterns

This page focuses on the technical standards and recurring patterns for .NET 10 development in InventoryAlert. For a high-level overview of how to test the system, see the **[Test Strategy & Guide](./test-strategy.md)**.

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

---

## Common Patterns

### Transaction Capture Pattern

Ensure results are captured *inside* the transaction block to avoid returning stale or blank entities.

```csharp
// ✅ GOOD — result assigned inside the lambda
PortfolioPositionResponse result = null!; 
await _unitOfWork.ExecuteTransactionAsync(async () => {
    var updated = await _repo.UpdateAsync(entity);
    result = MapToResponse(updated);
}, ct);
return result;
```

### Async Polling Helper (Tests)

Used in E2E tests to wait for background worker side-effects.

```csharp
await WaitHelper.WaitForConditionAsync(async () => {
    var res = await Client.GetAsync("/api/v1/notifications");
    return res.IsSuccessStatusCode;
}, timeoutSeconds: 15);
```

---

## Git Commit Convention

Follow the standard `type(scope): message` format.

| Type | Description |
|---|---|
| `feat` | New feature |
| `fix` | Bug fix |
| `test` | Adding or updating tests |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `docs` | Documentation only changes |
| `chore` | Maintenance tasks |

**Scopes**: `domain`, `infra`, `api`, `worker`, `ui`, `tests`.

*Example: `feat(api): add position bulk import`*
