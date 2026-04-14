# InventoryAlert Feature Audit & Consolidation Plan

## current Feature set

The system currently operates as a "Modern Financial Inventory Ecosystem" with the following features:

### 1. Core Inventory & Product Management
- **Description**: CRUD operations for products (tickers), tracking their properties and inventory levels.
- **Storage**: PostgreSQL.
- **Complexity**: High (tied to multiple services).

### 2. Market Data Synchronization
- **Description**: Background jobs (SyncPricesJob) that fetch real-time data from Finnhub.
- **Storage**: PostgreSQL (Current prices) and DynamoDB (Price history).
- **Complexity**: High (external API dependency, background scheduling).

### 3. Automated Alerting System
- **Description**: User-defined rules (AlertRules) triggered by price or inventory changes.
- **Workflow**: Price Sync -> SNS -> SQS -> Worker Evaluation -> Telegram.
- **Complexity**: Very High (distributed architecture via LocalStack/AWS services).

### 4. Market Intelligence (News, Earnings, Recommendations)
- **Description**: Supplemental financial data fetched from Finnhub.
- **Storage**: DynamoDB.
- **Complexity**: Medium-High (separate data path from core inventory).

### 5. Watchlist & Social
- **Description**: Users can "watch" tickers without necessarily having them in inventory.
- **Storage**: PostgreSQL.
- **Complexity**: Low.

### 6. System Observability & Auditing
- **Description**: Event tracking for all major actions and structured logging to Seq.
- **Storage**: PostgreSQL (SystemEvents) and Seq.
- **Complexity**: Medium.

---

## Suggested Removals (To "Make the App Smaller")

To reduce the footprint (infrastructure and code), I suggest the following:

### 1. 🗑️ Remove Market Intelligence (News, Earnings, Recommendations)
- **Reason**: These are secondary "nice-to-have" features that significantly increase the infrastructure complexity (DynamoDB dependency).
- **Impact**: Migration Complete.

### 2. Consolidation Roadmap (Lean Profile)

| Feature | Current State | Recommendation | Actions Taken |
| :--- | :--- | :--- | :--- |
| **Watchlist Service** | 🗑️ Removed | **Deprecate** | Merged into `Product` via `IsWatchOnly` & `UserId`. |
| **Price History (Dynamo)** | 💾 Migrated | **Re-Architect** | Moved to PostgreSQL; removed DynamoDB repo. |
| **Event Messaging (SNS)** | 📡 Removed | **Streamline** | Switched to direct SQS (`IQueueService`). |
| **Market News (Dynamo)** | 📰 Retained | **Keep (AWS Practice)** | Maintained for AWS resource skills. |
| **Per-User Isolation** | ✅ Enforced | **Strict Ownership** | All endpoints now filter by `UserId`. |

## 3. Post-Implementation Summary
The Inventory Management System has been optimized for a **Lean** profile:
1. **Relational Core**: All critical business data (Products, History, Alerts) is now in **PostgreSQL**.
2. **Simplified Messaging**: SNS fan-out removed in favor of direct SQS polling, reducing AWS footprint.
3. **Unified Model**: Eliminated the redundant `Watchlist` entity; users now "watch" products in their consolidated inventory.
4. **Strict Security**: Implemented ownership checks at the repository level; users can no longer access other users' data.

---

## Recommended "Lean" Feature Set
1. **Unified Product/Ticker Management** (Postgres)
2. **Simplified Price Sync** (Hangfire + Finnhub)
3. **In-Process Alerting** (MediatR/Background Task)
4. **Basic Transaction History**
5. **System Event Auditing**
