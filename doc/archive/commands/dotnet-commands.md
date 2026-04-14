# .NET CLI Quick Reference

This guide provides the essential commands to build, manage, and run the **Automated Inventory Alert System** project, including solution and test project setup.

## 🏗️ 1. Project Initialization (Solution & API)

Create the solution and a new Web API project with Controllers:
```bash
# Create the Solution
dotnet new sln -n InventoryManagementSystem

# Create the API Project
dotnet new webapi -n InventoryAlert.Api -o InventoryAlert.Api --use-controllers --no-https

# Add API Project to Solution
dotnet sln add InventoryAlert.Api/InventoryAlert.Api.csproj
```

## 🧪 2. Unit Testing Setup

Add a separate test project and link it to the API:
```bash
# Create the xUnit Test Project
dotnet new xunit -n InventoryAlert.Tests -o InventoryAlert.Tests

# Add Test Project to Solution
dotnet sln add InventoryAlert.Tests/InventoryAlert.Tests.csproj

# Reference the API project from the Test Project
dotnet add InventoryAlert.Tests/InventoryAlert.Tests.csproj reference InventoryAlert.Api/InventoryAlert.Api.csproj

# Add Testing Packages (Moq, Mvc.Testing)
dotnet add InventoryAlert.Tests/InventoryAlert.Tests.csproj package Moq
dotnet add InventoryAlert.Tests/InventoryAlert.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing
```

## 📦 3. Dependency Management (Core Packages)

Add the required NuGet packages to the API project:
```bash
# Navigate to the API project directory
cd InventoryAlert.Api

# Core PostgreSQL & EF Core Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design

# Hangfire for Job Management
dotnet add package Hangfire.AspNetCore
dotnet add package Hangfire.PostgreSql

# Motor for SNS/SQS (If applicable)
dotnet add package Motor.Extensions.Hosting.CloudEvents //TODO: LATER
```

## 🗄️ 4. Database Migrations (EF Core)

Note: Run these from the project root using `--project`:
```bash
# Create a migration
dotnet ef migrations add test --project InventoryAlert.Infrastructure --startup-project InventoryAlert.Api --context InventoryDbContext

dotnet ef migrations remove --project InventoryAlert.Infrastructure --startup-project InventoryAlert.Api --context InventoryDbContext

dotnet ef database update --project InventoryAlert.Infrastructure --startup-project InventoryAlert.Api --context InventoryDbContext
```

## 🛠️ 5. Build, Run, and Test

Core development commands:
```bash
# Build the entire solution
dotnet build

# Run all tests
dotnet test

# Run with Hot Reload (API only)
dotnet watch run --project InventoryAlert.Api/InventoryAlert.Api.csproj
```

## 🧹 6. Maintenance

Clean build artifacts and list packages:
```bash
dotnet clean
dotnet list package
```

## 📦 7. Deployment

Build for production:
```bash
dotnet publish InventoryAlert.Api/InventoryAlert.Api.csproj -c Release -o ./publish
```
