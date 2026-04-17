---
description: End-to-end development flow for a new feature — from requirement to merged code
type: workflow
status: active
version: 2.0
tags: [workflow, feature, ddd, lifecycle, inventoryalert]
---

// turbo-all

# /feature-flow — Master Development Lifecycle

**Objective**: Implement any feature end-to-end across all DDD layers. Every new feature follows this exact sequence — do not skip phases.

```
Requirement → Domain → Application → Infrastructure → Web → Tests → Review → Done
```

---

## Phase 0: Context Retrieval (BM25)

// turbo

1. **Pull DDD architecture rules**:
   ```bash
   python .agents/scripts/core/bm25_search.py "ddd-architecture clean-architecture boundaries" -n 2 -f ".agents/skills"
   ```

2. **Pull async execution & background worker rules**:
   ```bash
   python .agents/scripts/core/bm25_search.py "inventoryalert-async-patterns csharp-async" -n 2 -f ".agents/skills"
   ```

3. **Find tech debt + related service logic**:
   ```bash
   python .agents/scripts/core/bm25_search.py "<feature keyword> service tech debt bug" -n 3
   ```

---

## Phase 1: Understand the Requirement

1. Write a 1-sentence summary of what the feature does.
2. Identify which layers change:
   - New entity? → all layers
   - New behavior on existing entity? → Application + Web + Tests
   - New external call? → Infrastructure + Application
3. Determine if a DB migration is needed.

**Gate**: You can name every file that will change before writing code.

---

## Phase 2: Domain (Model First)

1. Create/update `Domain/Entities/<Entity>.cs` — POCOs only, zero dependencies
2. Create `Domain/Interfaces/I<Entity>Repository.cs` (extends `IGenericRepository<T>`)
3. Domain events → `Domain/Events/<Event>.cs` if needed

**Gate**:
```bash
dotnet build InventoryManagementSystem/InventoryAlert.Api
```
Domain must compile with zero cross-layer imports.

---

## Phase 3: Application (Behaviour)

1. Create `Application/DTOs/<Entity>Request.cs` + `<Entity>Response.cs`
2. Add method to `Application/Interfaces/I<Entity>Service.cs`
3. Implement in `Application/Services/<Entity>Service.cs`:
   - Wrap all writes in `_unitOfWork.ExecuteTransactionAsync(async () => { ... }, ct)`
   - Return DTOs only via `private static MapToResponse()`
   - Assign inside the lambda, never capture `new Entity()` before it

**Gate**: Service tests written and passing (see Phase 6).

---

## Phase 4: Infrastructure (Data Access)

1. Create `Infrastructure/Persistence/Repositories/<Entity>Repository.cs`
2. Create `Infrastructure/Persistence/Configurations/<Entity>Configuration.cs`
3. Migration (if schema changed):
   ```bash
   cd InventoryManagementSystem
   dotnet ef migrations add <VerbNoun> --project InventoryAlert.Api --output-dir Infrastructure/Persistence/Migrations
   dotnet ef database update --project InventoryAlert.Api
   ```
4. External API client → `Infrastructure/External/` if Finnhub or new integration

**Gate**: Migration applies cleanly. Repository tests pass.

---

## Phase 5: Web (Endpoint)

1. Add action to `Web/Controllers/<Entity>sController.cs`
   - `CancellationToken ct` as last param on every action
   - `[ProducesResponseType]` for every HTTP status
   - Zero business logic
2. Register DI:
   - `Web/ServiceExtensions/ApplicationServiceExtensions.cs` → service
   - `Web/ServiceExtensions/InfrastructureServiceExtensions.cs` → repository

**Smoke test**:
```bash
docker-compose up --build -d
# open http://localhost:8080/swagger
```

**Gate**: All HTTP verbs return correct status codes in Swagger.

---

## Phase 6: Tests (Quality Gate)

```bash
# P0 — Service
dotnet test InventoryManagementSystem --no-build --filter "FullyQualifiedName~<Entity>ServiceTests"

# P1 — Controller
dotnet test InventoryManagementSystem --no-build --filter "FullyQualifiedName~<Entity>sControllerTests"

# Full suite
dotnet test InventoryManagementSystem --no-build
```

**Required per new method**:
- [ ] Happy path
- [ ] Not-found / null edge case
- [ ] `ExecuteTransactionAsync` called `Times.Once` (for writes)
- [ ] Bug regression: entity not blank on transaction failure

**Gate**: `Failed: 0`

---

## Phase 7: Code Review

Run `/code-review` checklist. Key items:
- [ ] No `Console.WriteLine` → use `ILogger<T>`
- [ ] No `async` without `await` (CS1998)
- [ ] No business logic in controllers
- [ ] `ExecuteTransactionAsync` mock invokes delegate in tests

---

## Phase 8: Doc & Brain Freeze

Every single feature MUST be paired with a documentation file mapping its state. Check `/doc.md` for full format.
Write the structural markdown specification to `doc/<feature-id>_spec.md` starting with:

```markdown
---
description: <One-line summary>
type: spec
status: active
version: 1.0
tags: [spec, <feature_tags>]
---
```

```bash
# Re-index
python .agents/scripts/core/bm25_indexer.py
```

---

## Phase 9: Commit & Push

```bash
git add .
git commit -m "feat(<scope>): <short description>"
git push origin feature/<ticket>-<slug>
```

---

## Quick File Reference

| What | Where |
|------|-------|
| Entity | `Domain/Entities/` |
| Repo Interface | `Domain/Interfaces/` |
| DTOs | `Application/DTOs/` |
| Service Interface | `Application/Interfaces/` |
| Service Impl | `Application/Services/` |
| EF Config | `Infrastructure/Persistence/Configurations/` |
| Repo Impl | `Infrastructure/Persistence/Repositories/` |
| Migrations | `Infrastructure/Persistence/Migrations/` |
| External Client | `Infrastructure/External/` |
| Controller | `Web/Controllers/` |
| DI Extensions | `Web/ServiceExtensions/` |
| Tests | `InventoryAlert.Tests/<Layer>/` |

---

**Parent Context**: `GEMINI.md`
**Next Action**: `/doc` → `/code-review` → push PR
**Keywords**: `feature-flow`, `ddd`, `lifecycle`, `development`, `inventoryalert`
