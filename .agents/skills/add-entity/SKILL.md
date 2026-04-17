---
description: Add a new domain entity end-to-end following the DDD layer structure
type: workflow
status: active
version: 2.0
tags: [workflow, entity, ddd, domain, application, infrastructure, web, inventoryalert]
---

// turbo-all

# /add-entity — New Domain Entity End-to-End

**Objective**: Add a brand new aggregate root with its own table, repository, service, and endpoints across all DDD layers.

> **vs `/add-feature`**: Use `/add-feature` for adding behavior to an existing entity. Use this when you need a new table and a full CRUD surface.

---

## Phase 0: Retrieve Context (BM25)

// turbo

1. **Find layout and architecture rules from skills**:
   ```bash
   python .agents/scripts/core/bm25_search.py "ddd-architecture clean-architecture domain entity" -n 3 -f ".agents/skills"
   ```

2. **Find Postgres and EF Core definitions from skills**:
   ```bash
   python .agents/scripts/core/bm25_search.py "inventoryalert-efcore postgresql-table-design postgresql-optimization" -n 3 -f ".agents/skills"
   ```

3. **Find DI registration patterns**:
   ```bash
   python .agents/scripts/core/bm25_search.py "ServiceExtensions AddScoped repository service" -n 3
   ```

---

## Phase 1: Domain Entity

Create `Domain/Entities/<Entity>.cs`:

```csharp
namespace InventoryAlert.Api.Domain.Entities;

public class <Entity>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    // ... other properties
}
```

**Rules**: POCOs only. No EF, no services, no HTTP imports.

---

## Phase 2: Repository Interface

Create `Domain/Interfaces/I<Entity>Repository.cs`:

```csharp
using InventoryAlert.Api.Domain.Entities;

namespace InventoryAlert.Api.Domain.Interfaces;

public interface I<Entity>Repository : IGenericRepository<<Entity>>
{
    // Entity-specific query methods (if needed)
}
```

---

## Phase 3: DTOs

Create `Application/DTOs/<Entity>Request.cs`:
```csharp
public class <Entity>Request
{
    [Required] public string Name { get; set; } = string.Empty;
    // ... input properties, no Id
}
```

Create `Application/DTOs/<Entity>Response.cs`:
```csharp
public class <Entity>Response : <Entity>Request
{
    public int Id { get; set; }
}
```

---

## Phase 4: Service Interface

Create `Application/Interfaces/I<Entity>Service.cs`:

```csharp
public interface I<Entity>Service
{
    Task<IEnumerable<<Entity>Response>> GetAllAsync(CancellationToken ct);
    Task<<Entity>Response?> GetByIdAsync(int id, CancellationToken ct);
    Task<<Entity>Response> CreateAsync(<Entity>Request request, CancellationToken ct);
    Task<<Entity>Response> UpdateAsync(int id, <Entity>Request request, CancellationToken ct);
    Task<<Entity>Response?> DeleteAsync(int id, CancellationToken ct);
}
```

---

## Phase 5: Service Implementation

Create `Application/Services/<Entity>Service.cs`:

```csharp
public class <Entity>Service(
    IUnitOfWork unitOfWork,
    I<Entity>Repository repository) : I<Entity>Service
{
    // Implement all methods
    // Wraps writes in ExecuteTransactionAsync
    // Returns DTOs via private static MapToResponse()
    // NEVER captures new <Entity>() before a transaction lambda
}
```

---

## Phase 6: Repository Implementation

Create `Infrastructure/Persistence/Repositories/<Entity>Repository.cs`:

```csharp
public class <Entity>Repository(AppDbContext db)
    : GenericRepository<<Entity>>(db), I<Entity>Repository { }
```

---

## Phase 7: EF Configuration

Create `Infrastructure/Persistence/Configurations/<Entity>Configuration.cs`:

```csharp
public class <Entity>Configuration : IEntityTypeConfiguration<<Entity>>
{
    public void Configure(EntityTypeBuilder<<Entity>> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
    }
}
```

EF auto-discovers via `ApplyConfigurationsFromAssembly` in `AppDbContext`.

---

## Phase 8: Migration

```bash
cd InventoryManagementSystem
dotnet ef migrations add Add<Entity>Table --project InventoryAlert.Api --output-dir Infrastructure/Persistence/Migrations
dotnet ef database update --project InventoryAlert.Api
```

Review generated `Up()` and `Down()` before applying.

---

## Phase 9: DI Registration

`Web/ServiceExtensions/InfrastructureServiceExtensions.cs`:
```csharp
services.AddScoped<I<Entity>Repository, <Entity>Repository>();
```

`Web/ServiceExtensions/ApplicationServiceExtensions.cs`:
```csharp
services.AddScoped<I<Entity>Service, <Entity>Service>();
```

---

## Phase 10: Controller

Create `Web/Controllers/<Entity>sController.cs`:
- Route: `[Route("api/[controller]")]`
- `CancellationToken ct` on every action
- `[ProducesResponseType]` for every HTTP status
- Zero business logic

---

## Phase 11: Tests

Add in `InventoryAlert.Tests/`:
- `Application/Services/<Entity>ServiceTests.cs` — mock `IUnitOfWork` + `I<Entity>Repository`
- `Web/Controllers/<Entity>sControllerTests.cs` — mock `I<Entity>Service`

---

## Phase 12: Documentation & Verify

Generate an explicit documentation file in `doc/<EntityName>_spec.md` with the required schema:

```markdown
---
description: Specification for <Entity> including DDD and architecture implementations
type: spec
status: active
version: 1.0
tags: [spec, ddd, <entity>]
---

# <Entity> Specification

## Overview
(1-2 sentence purpose)

## Database Schema
(List columns and index strategies)

## Endpoints
(Route map of active endpoints)
```

Verify build and re-index:
```bash
dotnet build InventoryManagementSystem
dotnet test InventoryManagementSystem --no-build

# Re-index
python .agents/scripts/core/bm25_indexer.py
```

---

**Parent Context**: `GEMINI.md` · `.agents/skills/ddd-architecture/SKILL.md`
**Next Action**: `/doc` → `/code-review`
**Keywords**: `add-entity`, `domain`, `ddd`, `repository`, `service`, `migration`, `inventoryalert`
