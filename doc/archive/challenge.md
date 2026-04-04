# Coding Challenge: Automated Inventory Alert System

## 🎯 Objective

Build a C# Web API that monitors product stock levels and generates alerts when they fall below a certain threshold.

## 🏗️ Requirements

### 1. The Entity: `Product`

| Property          | Type        | Description                           |
| :---------------- | :---------- | :------------------------------------ |
| `Id`              | `int`       | Primary Key                           |
| `Name`            | `string`    | Unique name of the product            |
| `StockCount`      | `int`       | Current units available               |
| `AlertThreshold`  | `int`       | Trigger level for an alert            |
| `LastAlertSentAt` | `DateTime?` | Timestamp of the last processed alert |

### 2. Business Logic (API)

Implement a RESTful API to manage your inventory:

- `POST /api/products`: Add a new product.
- `PUT /api/products/{id}/stock`: Update the stock count.
- `GET /api/products/low-stock`: View all products currently below threshold.

### 3. The Engine (Background Job)

Implement a `BackgroundService` that:

- Runs every **5 minutes**.
- Scans the database for products where `StockCount <= AlertThreshold`.
- **Constraint**: To prevent log spam, only log the alert if `LastAlertSentAt` is null OR more than **1 hour ago**.
- Update the `LastAlertSentAt` once the alert is triggered/logged.

### 4. Persistence

- Use **PostgreSQL** with **Entity Framework Core**.

- Ensure migrations are applied successfully.

### 5. External Price Fetching (Finnhub)

Integrate with the **Finnhub Stock API** to:

- Add a property `TickerSymbol` to the `Product` entity.
- Implement a background process to fetch the current market price for each symbol.
- **Challenge**: If the market price is SIGNIFICANTLY higher than the original cost (e.g., > 20% increase), create a "High Value" alert even if stock is plentiful.

---

## 🛠️ Tech Stack & Standards

- **Framework**: .NET 8 or 9
- **DB**: Npgsql.EntityFrameworkCore.PostgreSQL

- **DI**: Remember to handle the **Scoped-in-Singleton** context issue correctly!
- **Clean Code**: Use Primary Constructors where possible.
- **Messaging**: Motor (for SNS/SQS abstraction) and Hangfire (for distributed job management).

---

## 🧠 Practice Tasks

1. **Project Initialization**: Build the standard Web API structure yourself.
2. **Concurrency**: What happens if the background job is running while an API request updates the stock? Consider how EF Core handles tracking.
3. **Resilience**: Add a `try-catch` inside your worker. If the service fails to connect to the database, it should back off and try again in 30 seconds.
4. **Distributed Messaging**: Implement **Motor** to push "Low Stock" events to **AWS SNS/SQS** topics.
5. **Robust Scheduling**: Move the inventory check job from `BackgroundService` to **Hangfire** to support persistence, retries, and a dashboard monitor.

_Note: Your previous work has been moved to the `/refer` folder for reference._
