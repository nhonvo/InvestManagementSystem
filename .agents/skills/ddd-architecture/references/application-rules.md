# Application Layer Workflows

### Transaction Usage
Every mutation operation (Create, Update, Delete) MUST be wrapped in the `IUnitOfWork` execution strategy to ensure atomicity.

**Critical Anti-Pattern (DO NOT USE)**:
Capturing a blank entity before the transaction scope and referencing it if the transaction fails creates silent data corruption responses.

```csharp
// ❌ WRONG:
Product updated = new();
await _unitOfWork.ExecuteTransactionAsync(async () => {
    updated = await _repo.UpdateAsync(requestEntity);
}, ct);
return MapToResponse(updated); // Returns completely blank Product on exception fallback
```

**Required Pattern**:
```csharp
// ✅ CORRECT:
ProductResponse result = null!;
await _unitOfWork.ExecuteTransactionAsync(async () => {
    var updated = await _repo.UpdateAsync(domainEntity);
    result = MapToResponse(updated); // Extracted only on success
}, ct);
return result;
```

### DTO Mapping
- Never return the raw `Product` Domain entity outside the Application layer.
- Use `private static` mapper methods inside the Service, such as `MapToResponse(Product product)` or `MapToEntity(ProductRequest request)`.
