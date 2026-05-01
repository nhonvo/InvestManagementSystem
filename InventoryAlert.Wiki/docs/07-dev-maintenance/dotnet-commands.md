# .NET CLI Reference

Detailed guide for managing the .NET projects within the InventoryAlert system.

## 1. Solution Commands

```bash
# Build entire solution
dotnet build

# Run clean
dotnet clean

# Run with Hot Reload (API only)
dotnet watch run --project InventoryAlert.Api/InventoryAlert.Api.csproj

# List all outdated packages
dotnet list package --outdated
```

---

## 2. Database Migrations (EF Core)

> Run from `InventoryManagementSystem/` directory.

```bash
# Create a migration
dotnet ef migrations add <MigrationName> \
  --project InventoryAlert.Api \
  --startup-project InventoryAlert.Api

# Remove last migration (before applying)
dotnet ef migrations remove \
  --project InventoryAlert.Api \
  --startup-project InventoryAlert.Api

# Apply migrations
dotnet ef database update \
  --project InventoryAlert.Api \
  --startup-project InventoryAlert.Api

# List all migrations
dotnet ef migrations list \
  --project InventoryAlert.Api \
  --startup-project InventoryAlert.Api
```

### Migration Naming Convention

```
Verb + Entity + Detail
Examples:
  InitialCreate
  AddStockListingTable
  AddTradeTableAndIndexes
  AddStockMetricAndEarningsTable
  AddInsiderTransactionTable
  AddNotificationTable
  AddCleanupIndexToPriceHistory
```

---

## 3. Test Commands

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"

# Run specific project
dotnet test InventoryAlert.UnitTests/
dotnet test InventoryAlert.IntegrationTests/

# Run with coverage (requires coverlet)
dotnet test --collect:"XPlat Code Coverage"

# Filter by test class
dotnet test --filter "ClassName=PortfolioServiceTests"

# Filter by test project type
dotnet test --filter "FullyQualifiedName~UnitTests"
dotnet test --filter "FullyQualifiedName~E2ETests"
```

### Code Coverage Script

A pre-configured batch script is available at the root of the repository to generate line-coverage metrics and an HTML report.

```powershell
# From the repository root
./code-coverage.bat
```

> **Requirements**: The script will automatically attempt to install `dotnet-reportgenerator-globaltool` and `coverlet.console` globally if they are not already present.

> ⚠️ **E2E tests** require the full Docker stack running: `docker compose up --build`

---

## 4. Package Management

```bash
# Add a package to a specific project
dotnet add InventoryAlert.Api/ package <PackageName>
dotnet add InventoryAlert.Domain/ package <PackageName>
dotnet add InventoryAlert.Infrastructure/ package <PackageName>

# Restore all packages
dotnet restore

# Key packages used
# Domain:       FluentValidation
# Infrastructure: Npgsql.EntityFrameworkCore.PostgreSQL, AWSSDK.DynamoDBv2, AWSSDK.SQS, StackExchange.Redis
# Api:          Hangfire.AspNetCore, Hangfire.PostgreSql, Serilog.AspNetCore, BCrypt.Net-Next
# Test:         xunit, Moq, FluentAssertions, RestSharp, coverlet.collector
```

---

## 5. Docker + .NET Hybrid Workflow

```bash
# Start only infra services (DB, Redis, Seq, Moto) — run API locally
docker compose up inventory-db inventory-cache inventory-seq inventory-moto -d

# Then run API locally with hot-reload
dotnet watch run --project InventoryAlert.Api

# Or run everything via Docker
docker compose up --build
```

---

## 6. Publish for Production

```bash
dotnet publish InventoryAlert.Api/InventoryAlert.Api.csproj \
  -c Release \
  --no-restore \
  -o ./publish/api

dotnet publish InventoryAlert.Worker/InventoryAlert.Worker.csproj \
  -c Release \
  --no-restore \
  -o ./publish/worker
```
