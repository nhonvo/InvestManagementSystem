# Alert Dispatch Flow

> How the system evaluates alert rules and delivers real-time in-app notifications.

## Overview

Alert dispatch uses a **hybrid evaluation pipeline** combining scheduled and event-driven triggers. Both paths use a shared `IAlertRuleEvaluator` for consistent business logic, and delivery is handled via **SignalR** with a **Redis backplane** for instant UI reactivity.

---

## Hybrid Evaluation Pipeline

The system ensures alert rules are checked whenever relevant data changes.

1.  **Scheduled Path (`SyncPricesJob`)**: Runs on a cron schedule (config: `WorkerSettings.Schedules.SyncPrices`). Scans symbols, fetches quotes, evaluates rules, persists notifications, and pushes via SignalR.
2.  **Event Path (`MarketPriceAlertHandler`)**: Triggered by SQS events (EventType: `inventoryalert.pricing.price-drop.v1`) for a specific symbol + price.
3.  **Holdings Path (`LowHoldingsHandler`)**: Triggered by SQS events (EventType: `inventoryalert.inventory.stock-low.v1`) when a user’s holdings drop below a threshold.

### Shared Evaluator Logic (`IAlertRuleEvaluator`)

```mermaid
flowchart TD
    A[Trigger: Price Update or Trade] --> B[Load active AlertRules for symbol/user]
    B --> C{For each AlertRule}
    C --> G{Condition breached?}
    G -- PriceAbove/Below --> H[Direct Comparison]
    G -- PercentDropFromCost --> I[Compute cost basis from Trade ledger]
    G -- LowHoldingsCount --> J[SUM Buy - SUM Sell from Trade ledger]
    
    H & I & J --> K{Breached?}
    K -- Yes --> L{Redis Cooldown active?}
    L -- Yes --> M[Skip — inventoryalert:alerts:cooldown:v1:{userId}:{ruleId} exists]
    L -- No --> N[1. INSERT Notification row]
    N --> O[2. SET inventoryalert:alerts:cooldown:v1:{userId}:{ruleId} TTL=24h]
    O --> P[3. Push via SignalR HubContext]
    P --> Q{TriggerOnce?}
    Q -- Yes --> R[UPDATE AlertRule SET IsActive = false]
    Q -- No --> C
```

---

## Real-Time Delivery (SignalR)

SignalR provides an instant push architecture:

1.  **Hub Host**: `InventoryAlert.Api` hosts the `NotificationHub` at `/hubs/notifications`.
2.  **The Signal**: When the Worker (producer) identifies a breach, it calls `NotifyAsync` on the `IAlertNotifier`.
3.  **The Relay**: The notifier publishes the `NotificationResponse` to the **Redis Backplane**.
4.  **The Push**: Redis relays the message to all Api instances. The instance holding the user's connection pushes the JSON payload over the **WebSocket** tunnel.
5.  **The UI**: The Next.js `NotificationProvider` receives the event and updates the navbar badge instantly.

---

## Notification Schema

Notifications are now categorized for better UX:

| Field | Detail |
|---|---|
| **Type** | `Price`, `Holdings`, `System`, `News` |
| **Severity** | `Info`, `Warning`, `Critical` |
| **Status** | Real-time state maintained via React Context + SignalR. |

---

## Deduplication & Cooldown

To prevent "Alert Storms," the system enforces a cooldown gate:
- **Key Pattern**: `inventoryalert:alerts:cooldown:v1:{userId}:{ruleId}`
- **Standard TTL**: 24 hours (set by the shared `IAlertRuleEvaluator`)
- **SQS message idempotency**: `msg:processed:{messageId}` (24h TTL) prevents processing the same SQS message multiple times in the native polling worker.
