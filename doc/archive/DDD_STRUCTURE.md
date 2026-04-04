# Pragmatic DDD: File Structure Guide

This document outlines the **Domain-Driven Design (DDD)** structure for the **Automated Inventory Alert System**. Our goal is to apply DDD principles while keeping the project "Small & Pragmatic" by consolidating layers into logical namespaces within a single API project, rather than over-engineering with multiple assembly projects.

---

## 🏗️ The Single-Project DDD Layout

```text
InventoryAlertSystem/
├── InventoryAlert.Api/
│   ├── Domain/                 # Pure Business Logic (No dependencies on external libraries)
│   │   ├── Entities/           # Core models (Product.cs)
│   │   ├── ValueObjects/       # Immutable data structures (Price, Stock)
│   │   ├── Interfaces/         # Repository contracts & External service interfaces
│   │   └── Events/             # Domain events (LowStockDetected.cs)
│   │
│   ├── Application/            # Use Case & Logic Orchestration
│   │   ├── Features/           # Functional slices (Inventory, Pricing, Notifications)
│   │   ├── Services/           # Logic that coordinates Domain logic
│   │   └── DTOs/               # Formats for API Request/Response
│   │
│   ├── Infrastructure/         # Implementation details (External worlds)
│   │   ├── Persistence/        # EF Core, AppDbContext, Migrations, Repositories
│   │   ├── External/           # Finnhub API Client, HTTP Clients
│   │   └── Messaging/          # Motor implementation, SNS/SQS, Hangfire Jobs
│   │
│   ├── Web/                    # Entry Point & API Configuration
│   │   ├── Controllers/        # REST Endpoints
│   │   ├── Middleware/         # Global Exception handling, Auth
│   │   └── Program.cs          # DI registration & App Startup
│   │
│   └── appsettings.json
│
└── InventoryAlert.Tests/        # Shadow the same structure for unit/integration tests
```

---

## 🧩 Architectural Breakdown

### 1. Domain (The Heart)
- **What it solves:** Architecture & Business Logic.
- **Rules:** Must NOT reference any external library (EF Core, Hangfire, etc.).
- **Content:** The `Product` entity lives here. It should contain logic like `bool IsBelowThreshold()` or `void UpdateStock(int amount)`.

### 2. Application (The Orchestrator)
- **What it solves:** Predictability.
- **Rules:** Depends ONLY on the Domain.
- **Content:** If a background job needs to check stock and send an alert, the `InventoryMonitorService` orchestrates this by using the Domain entities and Infrastructure interfaces.

### 3. Infrastructure (The "Details")
- **What it solves:** Maintainability & Security.
- **Rules:** Holds the "How". How do we save to PostgreSQL? How do we talk to Finnhub?
- **Content:** `AppDbContext`, `ProductRepository`, and the specific `FinnhubClient` implementation.

### 4. Web (The Gateway)
- **What it solves:** Performance & Interaction.
- **Content:** Handles HTTP requests, JSON serialization, and project-wide configuration.

---

## 🚀 Why This Solution?

1.  **Big Problem Solved (Architecture):** Prevents the "Fat Controller" and "Transaction Script" anti-patterns. By moving logic into the **Domain**, we ensure the system is testable without a database.
2.  **YAGNI (You Ain't Gonna Need It):** Traditional DDD suggests 4+ separate C# projects (`Company.Project.Domain`, `Company.Project.Infra`, etc.). For a small project, this adds build time and complexity. Folder-based DDD provides the **organizational benefits** without the **overhead**.
3.  **Modular Growth:** If the "Pricing Module" grows too large, it is already logically separated in the `Application/Features` and `Infrastructure/External` folders, making it easy to extract into a Microservice later if needed.
