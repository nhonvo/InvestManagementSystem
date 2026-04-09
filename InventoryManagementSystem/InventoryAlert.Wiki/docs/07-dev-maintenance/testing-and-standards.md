# Testing & Coding Standards

## Testing Stack

- **xUnit** — Test runner
- **Moq** — Mocking framework
- **FluentAssertions** — Human-readable assertions

## Test Naming Convention

```
MethodName_StateUnderTest_ExpectedBehavior
// Examples:
CreateAlert_ValidInput_AddsRuleAndPublishesEvent
GetProduct_NotFound_ReturnsNull
SyncPrices_FinnhubReturnsZero_SkipsUpdate
```

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

### Audit Verification
Any service method that modifies internal state must verify that mandatory audit logs are created:

```csharp
_stockTxRepo.Verify(r => r.AddAsync(It.Is<StockTransaction>(t => 
    t.ProductId == 1 && 
    t.Quantity == 50 && 
    t.UserId == "expected-user-id"), 
    It.IsAny<CancellationToken>()), 
    Times.Once);
```

### Running Tests

```bash
dotnet test InventoryAlert.UnitTests/
```

---

## C# Coding Standards

| Rule | Description |
|---|---|
| **Primary Constructors** | Use C# 12 primary constructors across all layers |
| **`CancellationToken ct`** | Must be the last parameter in every async method |
| **No `async` without `await`** | Remove `async` keyword and return `Task.FromResult(...)` instead |
| **`string.Empty`** | Use over `""` for empty string defaults on entity properties |
| **`null!` suppressor** | Forbidden unless accompanied by a comment explaining why |
| **Private fields** | `_camelCase`. Everything else: `PascalCase` |
| **`ILogger<T>`** | Use for all logging. `Console.WriteLine` is banned |
| **`AsNoTracking()`** | Required on all read-only EF Core queries |

## Transaction Capture Pattern

```csharp
// ❌ BAD — blank entity returned if lambda throws
Product updated = new();
await _unitOfWork.ExecuteTransactionAsync(async () => {
    updated = await _repo.UpdateAsync(entity);
}, ct);
return MapToResponse(updated); // may map a blank Product!

// ✅ GOOD — result assigned inside the lambda
ProductResponse result = null!; // null-forgiving acceptable here: assigned before read
await _unitOfWork.ExecuteTransactionAsync(async () => {
    var updated = await _repo.UpdateAsync(entity);
    result = MapToResponse(updated);
}, ct);
return result;
```
