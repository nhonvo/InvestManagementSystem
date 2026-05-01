---
description: Code review checklist before merging any feature branch
type: workflow
status: active
version: 2.0
tags: [workflow, code-review, quality, checklist, inventoryalert]
---

// turbo-all

# /code-review — Pre-Merge Quality Gate

**Objective**: Verify every layer meets the project's architecture, quality, and test standards before merging.

---

## Phase 0: Retrieve Standards (BM25)

// turbo

1. **Pull clean architecture and framework rules**:
   ```bash
   python .agents/scripts/core/bm25_search.py "ddd-architecture clean-architecture csharp-async" -n 3 -f ".agents/skills"
   ```

2. **Pull Postgres design & optimization standards**:
   ```bash
   python .agents/scripts/core/bm25_search.py "inventoryalert-efcore postgresql-optimization postgresql-table-design" -n 3 -f ".agents/skills"
   ```

---

## Phase 1: Domain Layer (`Domain/`)

- [ ] Entity has zero imports from Application, Infrastructure, or Web
- [ ] All string properties default to `= string.Empty` (no nullability suppressors)
- [ ] Repository interfaces declare only data-access contracts — no business logic

---

## Phase 2: Application Layer (`Application/`)

- [ ] Service injects only `IUnitOfWork` + `I<Entity>Repository` (no `AppDbContext`)
- [ ] **Every write** wrapped in `ExecuteTransactionAsync`
- [ ] Entity never returned raw — only via `MapToResponse()`
- [ ] `MapToResponse()` and `MapToEntity()` are `private static`
- [ ] `KeyNotFoundException` (not `ArgumentException`) when entity not found on update
- [ ] **No** default entity captured before a transaction lambda — assign inside the lambda:
  ```csharp
  // ❌ BAD
  AlertRule updated = new();
  await _unitOfWork.ExecuteTransactionAsync(async () => { updated = ...; }, ct);

  // ✅ GOOD
  AlertRuleResponse result = null!;
  await _unitOfWork.ExecuteTransactionAsync(async () => { result = MapToResponse(await ...); }, ct);
  ```

---

## Phase 3: Infrastructure Layer (`Infrastructure/`)

- [ ] No `async` keyword without `await` — CS1998 (fix: return `Task.FromResult(...)`)
- [ ] Read-only EF queries use `.AsNoTracking()`
- [ ] `FinnhubClient` handles null/failure defensively — never throws on external error
- [ ] Errors logged via `ILogger<T>`, not `Console.WriteLine`
- [ ] `AppDbContext` injected only via `UnitOfWork` — never directly into services

---

## Phase 4: Web Layer (`Web/`)

- [ ] Controller actions call **only** service methods — zero business logic
- [ ] Every action has `CancellationToken ct` as last parameter
- [ ] Every action has `[ProducesResponseType]` for each HTTP status
- [ ] No hardcoded secrets — `appsettings.json` contains no real API keys

---

## Phase 5: Tests (`InventoryAlert.Tests/`)

- [ ] Every new service method has happy path + not-found + boundary tests
- [ ] `ExecuteTransactionAsync` mock **invokes** the delegate:
  ```csharp
  // ✅ Required pattern
  _unitOfWork.Setup(u => u.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), Ct))
             .Returns<Func<Task>, CancellationToken>((action, _) => action());
  ```
- [ ] Repository tests use EF InMemory with `Guid.NewGuid().ToString()` DB name
- [ ] Call counts verified with `Times.Once` / `Times.Never`
- [ ] No `Thread.Sleep`

---

## Phase 6: General

- [ ] No unused `using` directives
- [ ] No commented-out code
- [ ] All new packages have explicit version pinned in `.csproj`
- [ ] Run full test suite green:
  ```bash
  dotnet test InventoryManagementSystem --no-build
  ```
- [ ] Docker build passes:
  ```bash
  docker-compose build
  ```

---

## Phase 7: Brain Freeze

```bash
# Re-index with new code
python .agents/scripts/core/bm25_indexer.py
```

Sign off in PR description: `✅ /code-review Complete`

---

**Parent Context**: `GEMINI.md` · `.agents/rules/project-rules.md`
**Next Action**: Push PR → merge
**Keywords**: `code-review`, `quality`, `checklist`, `ddd`, `inventoryalert`
