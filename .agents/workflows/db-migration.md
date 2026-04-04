---
description: Create and apply an EF Core database migration
type: workflow
status: active
version: 2.0
tags: [workflow, migration, efcore, database, postgresql, inventoryalert]
---

// turbo-all

# /db-migration — EF Core Migration

**Objective**: Create, review, and apply an EF Core migration for schema changes in `InventoryAlert.Api`.

---

## Phase 0: Retrieve Context (BM25)

// turbo

1. **Pull Postgres optimization & table design rules**:
   ```bash
   python .agents/scripts/core/bm25_search.py "inventoryalert-efcore postgresql-optimization postgresql-table-design" -n 3 -f ".agents/skills"
   ```

2. **Find AppDbContext configuration & migration drift**:
   ```bash
   python .agents/scripts/core/bm25_search.py "AppDbContext DbSet migration" -n 2
   ```

---

## Prerequisites

- `dotnet-ef` installed: `dotnet tool install -g dotnet-ef`
- PostgreSQL running: `docker-compose up postgres -d`
- `appsettings.Development.json` has valid `ConnectionStrings.DefaultConnection`
- Working directory: solution root `InventoryManagementSystem/`

---

## Steps

### 1. Verify EF tool

```bash
dotnet ef --version
```

### 2. Create the migration

```bash
dotnet ef migrations add <MigrationName> --project InventoryAlert.Api --output-dir Infrastructure/Persistence/Migrations
```

Example:
```bash
dotnet ef migrations add AddStockAlertThresholdToProduct --project InventoryAlert.Api --output-dir Infrastructure/Persistence/Migrations
```

> **Naming convention**: PascalCase `Verb + Entity + Detail`
> Examples: `AddStockAlertThresholdToProduct` · `CreateOrderTable` · `RenameTickerSymbolColumn`

### 3. Review the generated migration

Open `Infrastructure/Persistence/Migrations/<timestamp>_<Name>.cs`:

- `Up()` — adds/changes exactly what you intended
- `Down()` — correctly reverses it
- **Never edit `ModelSnapshot.cs` manually**

### 4. Apply the migration

```bash
dotnet ef database update --project InventoryAlert.Api
```

### 5. Verify it applied

```bash
dotnet ef migrations list --project InventoryAlert.Api
```

New migration shows `(Applied)`.

### 6. Rollback if needed

```bash
dotnet ef database update <PreviousMigrationName> --project InventoryAlert.Api
dotnet ef migrations remove --project InventoryAlert.Api
```

---

## Notes

- Migrations run **automatically on startup** via `db.Database.Migrate()` in `Program.cs`
- Seed data lives in `AppDbContext.OnModelCreating` via `HasData()`
- For production: run migrations in CI, not at startup
- Connection string format:
  ```
  Host=localhost;Port=5432;Database=inventorydb;Username=postgres;Password=postgres
  ```

---

## Troubleshooting

| Error | Fix |
|-------|-----|
| `No DbContext was found` | Run from `InventoryManagementSystem/`, use `--project InventoryAlert.Api` |
| `Connection refused` | `docker-compose up postgres -d` |
| `Pending model changes` | Run `dotnet ef migrations add` to capture drift |
| `MSB4066 Include attribute` | Fix nested `<PackageReference>` in `.csproj` |

---

**Parent Context**: `GEMINI.md`
**Next Action**: Restart API to apply migration, then run `/run-tests`
**Keywords**: `migration`, `efcore`, `database`, `postgresql`, `schema`, `inventoryalert`
