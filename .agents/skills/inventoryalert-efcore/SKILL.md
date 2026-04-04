---
name: inventoryalert-efcore
description: Project-specific design and optimization rules for PostgreSQL tables managed via Entity Framework Core 10. Use this skill when modifying the database schema, writing complex EF Core LINQ queries, dealing with index/key configurations in the Infrastructure project, or working heavily with Npgsql optimizations.
---

# InventoryAlert PostgreSQL & EF Core Strategy

This skill outlines the Antigravity setup for managing PostgreSQL data safely through EF Core within the `InventoryAlert.Api.Infrastructure` namespace.

## Schema Configuration
We actively separate Entity classes (POCOs) from schema configuration. Do not clutter `Product.cs` with database mapping attributes like `[Table]` or `[Column]`. 

Instead, place all constraints in the `Infrastructure/Persistence/Configurations/` directory using `IEntityTypeConfiguration<T>`.

*   **String Defaults:** Postgres treats empty strings and `null` string distinctively. Always ensure strings default to `string.Empty` domain-side unless configured to allow null schema-side.
*   **Precision Specs:** Decimal fields mapping to monetary values (like `CurrentPrice`) must have precision explicit formats: `builder.Property(x => x.CurrentPrice).HasPrecision(18, 4);`.

## Npgsql and EF Core Interactions

For advanced querying behaviors specifically tailored to the project setup, please refer to the progressive disclosure references:

- **Query Optimizations (.AsNoTracking):** See [references/read-optimizations.md](references/read-optimizations.md) for handling pure-read pipelines.
- **Migration & Tracking Checks:** See [references/migration-workflows.md](references/migration-workflows.md) to ensure schema commands successfully build within this specific Docker layer mapping.
