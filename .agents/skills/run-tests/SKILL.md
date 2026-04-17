---
description: Run unit tests with optional coverage report
type: workflow
status: active
version: 2.0
tags: [workflow, tests, xunit, coverage, inventoryalert]
---

// turbo-all

# /run-tests — Unit Test Execution

**Objective**: Run the xUnit test suite for `InventoryAlert.Tests` with optional coverage reporting.

---

## Phase 0: Retrieve Test Context (BM25)

// turbo

1. **Pull explicit testing architecture skills**:
   ```bash
   python .agents/scripts/core/bm25_search.py "testing-patterns csharp-xunit" -n 2 -f ".agents/skills"
   ```

2. **Find relevant test files**:
   ```bash
   python .agents/scripts/core/bm25_search.py "xunit moq fluent assertion test service controller" -n 5
   ```

---

## Prerequisites

- `InventoryManagementSystem/` as working directory
- Packages: `xunit`, `Moq`, `FluentAssertions`, `EF InMemory` (all in `.csproj`)

---

## Steps

### 1. Restore & build

```bash
dotnet restore
dotnet build --no-restore
```

### 2. Run all tests

```bash
dotnet test InventoryManagementSystem --no-build --verbosity normal
```

### 3. Run by test class

```bash
dotnet test InventoryManagementSystem --no-build --filter "FullyQualifiedName~ProductServiceTests"
dotnet test InventoryManagementSystem --no-build --filter "FullyQualifiedName~ProductsControllerTests"
dotnet test InventoryManagementSystem --no-build --filter "FullyQualifiedName~GenericRepositoryTests"
```

### 4. Run by method name

```bash
dotnet test InventoryManagementSystem --no-build --filter "FullyQualifiedName~GetAll_ReturnsEmptyList_WhenNoProductsExist"
```

### 5. Run with coverage (HTML report)

```bash
dotnet test InventoryManagementSystem --no-build --collect:"XPlat Code Coverage" --results-directory ./TestResults
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" -targetdir:"./TestResults/CoverageReport" -reporttypes:Html
start ./TestResults/CoverageReport/index.html
```

---

## Coverage Targets

| Layer | Target |
|-------|--------|
| Application/Services | ≥ 90% |
| Web/Controllers | ≥ 85% |
| Infrastructure/Repositories | ≥ 80% |

---

## Expected Output

```
Passed: 43  Failed: 0  Skipped: 0
```

## Troubleshooting

| Error | Fix |
|-------|-----|
| NuGet version conflict | Align `Microsoft.EntityFrameworkCore.InMemory` to `10.0.5` |
| `ProductFixtures` type not found | Add `<ProjectReference>` to `InventoryAlert.Api.csproj` in test `.csproj` |
| `CS1998` in test build | Remove `async` from `GenericRepository` non-awaiting methods |
| Test isoloation failure | Ensure EF InMemory tests use `Guid.NewGuid().ToString()` as DB name |

---

**Parent Context**: `.agents/GEMINI.md` · `.agents/skills/testing-patterns/SKILL.md`
**Next Action**: If all pass → `/code-review`; if failures → `/fix-build`
**Keywords**: `tests`, `xunit`, `moq`, `fluentassertions`, `coverage`, `inventoryalert`
