# Alert Dispatch Flow

> How the system evaluates alert rules and delivers real-time in-app notifications.

## Overview

Alert dispatch in InventoryAlert **v3** uses a **hybrid evaluation pipeline** combining scheduled and event-driven triggers. Both paths utilize a shared `IAlertRuleEvaluator` to ensure consistent business logic. Delivery is now handled via **SignalR** with a **Redis Backplane** for instant UI reactivity.

---

## Hybrid Evaluation Pipeline

The system ensures alert rules are checked whenever relevant data changes.

1.  **Scheduled Path (`SyncPricesJob`)**: Runs every 15 minutes. Performs a global scan of all active stock listings and evaluates all associated rules.
2.  **Event Path (`MarketPriceAlertHandler`)**: Triggered immediately by price update events from SQS.
3.  **Holdings Path (`LowHoldingsHandler`)**: Triggered immediately when a trade is recorded.

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
    L -- Yes --> M[Skip — alert:cooldown:{userId}:{ruleId} exists]
    L -- No --> N[1. INSERT Notification row]
    N --> O[2. SET alert:cooldown:{userId}:{ruleId} TTL=1h+]
    O --> P[3. Push via SignalR HubContext]
    P --> Q{TriggerOnce?}
    Q -- Yes --> R[UPDATE AlertRule SET IsActive = false]
    Q -- No --> C
```

---

## Real-Time Delivery (SignalR)

InventoryAlert v3 replaces legacy polling with an instant push architecture:

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
- **Key Pattern**: `alert:cooldown:{userId}:{ruleId}`
- **Standard TTL**: 
    - Price Alerts: 1 Hour
    - Holdings Alerts: 6 Hours
    - News Alerts: 24 Hours
- **Global SQS Dedup**: `dedup:sqs:{messageId}` (30 min TTL) prevents duplicate processing of the same message across worker nodes.
