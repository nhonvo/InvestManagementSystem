# Business Logic & Rules

This document outlines the core business logic and rules governing the Inventory Alert System.

## 1. Alerting Logic

### 1.1 Price Drop Alerts
Alerts are triggered when a product's current price drops below its original price by a specific percentage.

- **Formula**: `(CurrentPrice - OriginPrice) / OriginPrice`
- **Default Threshold**: 10% (0.1) drop.
- **Cooldown**: 1 hour. A new alert will not be sent for the same product within 1 hour of the previous alert to prevent notification fatigue.
- **Source**: Finnhub Real-time Quotes.

- **Trigger**: `StockCount <= StockAlertThreshold`
- **Action**: A notification is dispatched via configured channels (Telegram, SNS).

### 1.3 Inventory Audit Trail
Every manual stock update performed via the API is tracked for audit and accountability.

- **Entity**: `StockTransaction`
- **Mechanism**: When `UpdateStockCountAsync` is called, the system calculates the difference (`newCount - currentCount`).
- **Classification**:
    - `diff > 0`: Marked as `Restock`.
    - `diff < 0`: Marked as `Adjustment`.
- **Metadata**: Each record captures the `UserId` of the requester, a `Timestamp`, and a `Reference` note.
- **Transactionality**: Both the product stock update and the audit record insertion occur within a single Unit of Work transaction.

## 2. Data Synchronization

### 2.1 Real-time Price Sync
The system synchronizes stock prices from Finnhub to provide up-to-date monitoring.

- **Parallelism**: The API and Worker use a `SemaphoreSlim(5)` pattern to process up to 5 symbols in parallel, keeping within Finnhub's rate limits while improving performance.
- **Backoff**: If Finnhub returns null or zero for a symbol, the system logs a warning and skips the update for that cycle.

### 2.2 Symbol Validation
To reduce unnecessary external calls, the system validates stock symbols before adding them to a user's watchlist.

- **Caching**: Validation results (Valid/Invalid) are cached in Redis for **24 hours**.

## 3. Background Processing

The system relies on Hangfire for recurring tasks:
- **Price Sync Job**: Fetches latest prices for all active products.
- **Market News Job**: Scrapes latest market news into DynamoDB.
- **Earnings/Recommendations**: Synchronizes analyst and financial data periodically.

## 4. User Roles
- **Admin**: Full access to product management and system triggers.
- **User**: Protected access to personal watchlists and alert rules.
