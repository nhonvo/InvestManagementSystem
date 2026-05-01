# Domain Layer Construction Rules

### Entity Guidelines
Entities are POCOs (Plain Old CLR Objects). They hold data and state, but in this specific architecture, they are kept lean.

- **Initialization**: Strings must default to `string.Empty` to prevent nullability bugs.
- **Encapsulation**: Properties generally use `{ get; set; }` to play nicely with EF Core serialization.
- **No Null Suppressions**: Do not use `null!` unless absolutely unavoidable (and strongly document why).

### Repository Interfaces
Interfaces define *what* the application needs to store, not *how*.
```csharp
namespace InventoryAlert.Domain.Interfaces;

public interface ITradeRepository : IGenericRepository<Trade>
{
    // Place strictly domain-relevant extraction queries here.
    // e.g. Task<List<Trade>> GetByUserAndSymbolAsync(Guid userId, string symbol, CancellationToken ct);
}
```
