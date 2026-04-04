---
description: Add a new application feature (new behavior on an existing entity)
type: workflow
status: active
version: 2.0
tags: [workflow, feature, application, service, controller, inventoryalert]
---

// turbo-all

# /add-feature — Add New Behavior to Existing Entity

**Objective**: Add a new method + endpoint to an existing entity without touching Domain or creating a new table.

> **vs `/add-entity`**: Use this when you're adding behavior (method/endpoint) to an entity that already exists. Use `/add-entity` when you need a new table.

---

## Phase 0: Retrieve Context (BM25)

// turbo

1. **Pull async execution & testing skills**:
   ```bash
   python .agents/scripts/core/bm25_search.py "testing-patterns csharp-xunit inventoryalert-async-patterns csharp-async" -n 3 -f ".agents/skills"
   ```

2. **Find existing service methods** for the target entity:
   ```bash
   python .agents/scripts/core/bm25_search.py "<entity> service method async" -n 5
   ```

3. **Check tech debt** — avoid blank-entity capture bug:
   ```bash
   python .agents/scripts/core/bm25_search.py "ExecuteTransactionAsync blank entity capture bug" -n 2
   ```

---

## Phase 1: Define the Behavior

Answer before writing code:
- What **input** does it receive? (new DTO / existing DTO / query param)
- What **output** does it return? (new shape / existing shape / void)
- Does it **read** or **write** (or both)?
- Does it call **Finnhub**?
- Does it need a **migration**? (new column → use `/db-migration`)

---

## Phase 2: Add to Service Interface

Open `Application/Interfaces/I<Entity>Service.cs`:

```csharp
/// <summary>One-line description of what this does.</summary>
Task<YourOutputDto> YourNewMethodAsync(InputParam param, CancellationToken cancellationToken);
```

---

## Phase 3: Add DTO(s) if Needed

Only create a new DTO if the shape differs from existing ones:

```
Application/DTOs/<NewRequest>.cs    ← if new input shape
Application/DTOs/<NewResponse>.cs  ← if new output shape
```

---

## Phase 4: Implement in Service

Open `Application/Services/<Entity>Service.cs`.

**Read-only**:
```csharp
public async Task<YourOutputDto> YourNewMethodAsync(InputParam param, CancellationToken ct)
{
    var data = await _repo.GetAllAsync(ct);
    return data.Where(...).Select(MapToYourDto).ToList();
}
```

**Write (mutation)**:
```csharp
public async Task<YourOutputDto> YourNewMethodAsync(int id, InputParam param, CancellationToken ct)
{
    var entity = await _repo.GetByIdAsync(id, ct)
        ?? throw new KeyNotFoundException($"...");

    entity.Field = param.Value;

    YourOutputDto result = null!;
    await _unitOfWork.ExecuteTransactionAsync(async () =>
    {
        var updated = await _repo.UpdateAsync(entity);
        result = MapToYourOutputDto(updated);  // ← assign INSIDE lambda, not before
    }, ct);

    return result;
}
```

**Finnhub call**:
```csharp
var quote = await _finnhubClient.GetQuoteAsync(entity.TickerSymbol, ct);
if (quote?.CurrentPrice is null or 0) return null;
```

---

## Phase 5: Add Repository Method (if needed)

If GenericRepository doesn't cover the query, add to:
- `Domain/Interfaces/I<Entity>Repository.cs` — interface
- `Infrastructure/Persistence/Repositories/<Entity>Repository.cs` — EF impl (use `.AsNoTracking()` for reads)

---

## Phase 6: Migration (if needed)

Only if a new column/index is required:
```bash
cd InventoryManagementSystem
dotnet ef migrations add <VerbNoun> --project InventoryAlert.Api --output-dir Infrastructure/Persistence/Migrations
dotnet ef database update --project InventoryAlert.Api
```

---

## Phase 7: Wire the Endpoint

Add to `Web/Controllers/<Entity>sController.cs`:

```csharp
[HttpGet("your-route")]
[ProducesResponseType(typeof(YourOutputDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> YourAction([FromQuery] InputParam param, CancellationToken ct)
{
    var result = await _service.YourNewMethodAsync(param, ct);
    return result is null ? NotFound() : Ok(result);
}
```

HTTP verb guide:
| Operation | Verb | Example Route |
|-----------|------|---------------|
| Read collection | `GET` | `api/products` |
| Read by id | `GET` | `api/products/{id}` |
| Computed report | `GET` | `api/products/price-alerts` |
| Create | `POST` | `api/products` |
| Full replace | `PUT` | `api/products/{id}` |
| Partial update | `PATCH` | `api/products/{id}/stock` |
| Delete | `DELETE` | `api/products/{id}` |

---

## Phase 8: Write Tests

**Service test** — `InventoryAlert.Tests/Application/Services/<Entity>ServiceTests.cs`:
- Happy path
- Not-found / null path
- `ExecuteTransactionAsync` called `Times.Once` (for writes)

**Controller test** — `InventoryAlert.Tests/Web/Controllers/<Entity>sControllerTests.cs`:
- Correct HTTP status per branch

---

## Phase 9: Document & Verify

Create or update a specification in `doc/<FeatureName>_spec.md` appending the YAML frontmatter:

```markdown
---
description: Specification for <Behavior/Feature> attached to <Entity>
type: spec
status: active
version: 1.0
tags: [spec, feature, <entity>]
---

# <Feature> Implementation

## Flow
(Describe Finnhub integration, service map, and unit of work)

## Endpoints Modified
(Route map + Output Schema snippet)
```

```bash
# Run tests for this feature
dotnet test InventoryManagementSystem --no-build --filter "FullyQualifiedName~<EntityName>"

# Re-index docs
python .agents/scripts/core/bm25_indexer.py
```

Then run `/code-review` before pushing.

---

**Parent Context**: `GEMINI.md` · `.agents/skills/ddd-architecture/SKILL.md`
**Next Action**: `/doc` → `/code-review`
**Keywords**: `add-feature`, `service`, `controller`, `application`, `inventoryalert`
