# AI Agent Workflows

> Catalog of repository-specific patterns and available agent commands.

## Available Workflows

| Command | Purpose |
|---|---|
| `/add-entity` | Scaffold a new domain entity end-to-end (Domain → Infra → App → Web) |
| `/add-feature` | Add new behavior to an existing entity |
| `/feature-flow` | Full end-to-end development flow from requirement to code |
| `/db-migration` | Create and apply an EF Core database migration |
| `/run-tests` | Run unit tests with optional coverage report |
| `/code-review` | Pre-merge checklist for code quality |
| `/doc` | Scaffold and sync documentation |

## Key Patterns to Know

### Transaction Capture Pattern

```csharp
// ✅ GOOD — result assigned inside the lambda
ProductResponse result = null!;
await _unitOfWork.ExecuteTransactionAsync(async () => {
    var updated = await _repo.UpdateAsync(entity);
    result = MapToResponse(updated);
}, ct);
return result;
```

### Primary Constructor Injection (C# 12)

```csharp
public class ProductService(IProductRepository repo, IUnitOfWork uow) : IProductService
{
    public async Task<ProductResponse> GetAsync(int id, CancellationToken ct)
        => MapToResponse(await repo.GetByIdAsync(id, ct));
}
```
