# EF InMemory Integration

When testing `GenericRepository` or concrete EF code, using `Microsoft.EntityFrameworkCore.InMemory` requires careful state initialization. 

The InMemory database leaks state across test methods if they share the same context definition database name.

### Proper Initialization

Use `Guid.NewGuid().ToString()` so every test method runs in purely isolated memory space.

```csharp
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // 👈 Isolated
    .Options;

using var dbContext = new AppDbContext(options);

// Seed isolated state
dbContext.Products.Add(new Product { Name = "Test" });
await dbContext.SaveChangesAsync();

// Initialize subject and assert
var repo = new GenericRepository<Product>(dbContext);
var result = await repo.GetAllAsync(CancellationToken.None);
```
