# Project Manifest Automated Inventory Alert System

Component Definitive Name Technology Responsibility

---

Solution `InventoryAlertSystem` Main .NET Solution
Web API Project `InventoryAlert.Api` REST Endpoints (ASP.NET Core 9)
Core Entity `Product` Domain Model (EF Core)
Database `InventoryDb` PostgreSQL (Npgsql)
Job Scheduler `Hangfire` Background processing & retries
Messaging Motor `InventoryAlert.Messaging` AWS SNSSQS abstraction (Motor.NET)
External Service `FinnhubClient` Market price synchronization

## Module Definitions

1. Inventory Module
    - Handles CRUD operations for `Product`.
    - Manages stock level updates via `PUT apiproducts{id}stock`.
2. Notification Module
    - Evaluates `StockCount` against `AlertThreshold`.
    - Dispatches events to SQS via Motor.
    - Prevents alert fatigue by checking `LastAlertSentAt`.
3. Pricing Module
    - Integrates with the Finnhub API.
    - Periodically updates `CurrentMarketPrice` for products with a `TickerSymbol`.
    - Compares market value vs. cost to flag High Value assets.

### Integration Details

- SNS Topic `low-stock-alerts`
- SQS Queue `inventory-notification-queue`
- Dashboard Hangfire Dashboard enabled at `hangfire`.
