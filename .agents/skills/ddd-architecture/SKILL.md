---
name: ddd-architecture
description: DDD layer rules, conventions, and architectural boundaries for InventoryAlert.Api. Use this skill whenever placing new code, creating entities, building controllers, or navigating the boundary between the Domain, Application, Infrastructure, and Web layers.
---

# DDD Architecture for InventoryAlert.Api

This skill enforces the strict 4-layer Domain-Driven Design (DDD) architecture used in this project.

## Layer Dependencies & Rules

The fundamental rule: **Dependencies only point inwards.** 
`Web` ➔ `Application` ➔ `Domain`
`Infrastructure` ➔ `Domain`

### 1. Domain Layer (`Domain/`)
- **Mandate**: No dependencies. Represents pure business concepts.
- **Allowed**: Plain C# classes (Entities), Enums, Repository Interfaces, Custom Exceptions.
- **Forbidden**: EF Core references, HTTP logic, framework injection.
- **Reference**: See [references/domain-rules.md](references/domain-rules.md) for entity construction rules and defaults.

### 2. Application Layer (`Application/`)
- **Mandate**: Use Cases and workflow orchestration.
- **Allowed**: Service implementations, DTOs (Request/Response), Service Interfaces.
- **Forbidden**: Direct `AppDbContext` usage. Use `IUnitOfWork` and repository interfaces instead.
- **Reference**: See [references/application-rules.md](references/application-rules.md) for Transaction Patterns and DTO mapping strictness.

### 3. Infrastructure Layer (`Infrastructure/`)
- **Mandate**: Implement data access, external APIs, and I/O.
- **Allowed**: `DbContext`, `GenericRepository<T>`, `FinnhubClient`, `FinnhubPriceSyncWorker`.
- **Requirements**: Read-only queries must use `.AsNoTracking()`.
- **Reference**: See [references/infrastructure-rules.md](references/infrastructure-rules.md) for EF Core setups and CS1998 mitigation.

### 4. Web Layer (`Web/`)
- **Mandate**: Entry point and presentation. Thin controllers.
- **Allowed**: Controllers, DI Registration (`ServiceExtensions`), Configuration models (`AppSettings.cs`).
- **Forbidden**: Any business logic inside Controllers.

## File & Namespace Conventions

- **Namespaces**: E.g., `InventoryAlert.Api.Application.Services` (Match folder structure)
- **Formatting**: Primary constructors (C# 12) everywhere. `CancellationToken` as the last parameter in async methods.
