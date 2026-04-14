---
description: Diagnose and fix common build and test failures in this solution
type: workflow
status: active
version: 2.0
tags: [workflow, fix-build, diagnostics, errors, inventoryalert, dotnet]
---

// turbo-all

# /fix-build — Diagnose & Fix Build Failures

**Objective**: Quickly identify and fix common build, package, and test failures in `InventoryManagementSystem`.

---

## Phase 0: Retrieve Error Context (BM25)

// turbo

1. **Search for the error message**:
   ```bash
   python .agents/scripts/core/bm25_search.py "<error code or message>" -n 5
   ```

2. **Find known package issues**:
   ```bash
   python .agents/scripts/core/bm25_search.py "NuGet package version conflict EntityFrameworkCore" -n 3
   ```

---

## Quick Diagnostic

Run first:
```bash
cd InventoryManagementSystem
dotnet build 2>&1 | Select-String "error|Error"
```

---

## Error Catalog

---

### ❌ CS1998 — Async method lacks `await`
```
warning CS1998: This async method lacks 'await' operators and will run synchronously
```

**Where**: `GenericRepository.DeleteAsync`, `UpdateAsync`, `UpdateRangeAsync`

**Fix**:
```csharp
// Before (wrong)
public async Task<T> DeleteAsync(T entity) { ... }

// After (correct)
public Task<T> DeleteAsync(T entity)
{
    var result = _dbSet.Remove(entity);
    return Task.FromResult(result.Entity);
}
```

---

### ❌ NuGet version conflict
```
Found conflicts between different versions of "Microsoft.EntityFrameworkCore..."
```

**Fix**: Align all EF Core packages to `10.0.5`:
```bash
dotnet list InventoryAlert.Api/InventoryAlert.Api.csproj package --include-transitive | Select-String "EntityFrameworkCore"
```

In `InventoryAlert.Tests.csproj`:
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.5" />
```

---

### ❌ MSB4066 — `Include` attribute unrecognized
```
error MSB4066: The attribute "Include" in element <PackageReference> is unrecognized
```

**Cause**: A `<PackageReference>` nested inside another.

**Fix**: Open `.csproj` — every `<PackageReference>` must be a direct `<ItemGroup>` child:
```xml
<!-- ❌ BAD — nested -->
<PackageReference Include="A" Version="1.0">
    <PackageReference Include="B" Version="2.0" />
</PackageReference>

<!-- ✅ GOOD — flat -->
<PackageReference Include="A" Version="1.0" />
<PackageReference Include="B" Version="2.0" />
```

---

### ❌ `No DbContext was found` (EF migrations)
```
Unable to create an object of type 'AppDbContext'
```

**Fix**: Run from the correct directory with explicit flags:
```bash
cd InventoryManagementSystem
dotnet ef migrations add <Name> --project InventoryAlert.Api --output-dir Infrastructure/Persistence/Migrations
```

---

### ❌ `Connection refused` (PostgreSQL)
```
Npgsql.NpgsqlException: Failed to connect to 127.0.0.1:5432
```

**Fix**:
```bash
docker-compose up postgres -d
# Correct connection string:
# Host=localhost;Port=5432;Database=inventorydb;Username=postgres;Password=postgres
```

---

### ❌ `InvalidOperationException: AppSettings configuration is missing`

**Fix**: Create `appsettings.Development.json` from example:
```bash
cp InventoryAlert.Api/appsettings.Example.json InventoryAlert.Api/appsettings.Development.json
# Fill in Finnhub API key and DB connection string
```

---

### ❌ Test — `ProductFixtures` type not found
```
error CS0246: The type or namespace name 'ProductFixtures' could not be found
```

**Fix**: Ensure `InventoryAlert.Tests.csproj` has:
```xml
<ProjectReference Include="..\InventoryAlert.Api\InventoryAlert.Api.csproj" />
```

---

### ❌ Test — `ExecuteTransactionAsync` mock does nothing

**Symptom**: Tests pass but service writes are never called (`Times.Never` when expected `Times.Once`)

**Fix**: Mock must invoke the delegate:
```csharp
// ❌ BAD — action never runs
_unitOfWork.Setup(u => u.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), Ct))
           .Returns(Task.CompletedTask);

// ✅ GOOD — action runs
_unitOfWork.Setup(u => u.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), Ct))
           .Returns<Func<Task>, CancellationToken>((action, _) => action());
```

---

## After Fixing

```bash
dotnet build InventoryManagementSystem
dotnet test InventoryManagementSystem --no-build
```

Expected: `Failed: 0`

---

**Parent Context**: `GEMINI.md` · `.agents/skills/testing-patterns/SKILL.md`
**Next Action**: If build green → `/run-tests`
**Keywords**: `fix-build`, `CS1998`, `NuGet`, `MSB4066`, `efcore`, `mock`, `inventoryalert`
