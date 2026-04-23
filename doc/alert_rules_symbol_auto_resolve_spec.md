---
description: Allow creating alert rules without requiring prior symbol discovery.
type: spec
status: active
version: 1.0
tags: [alert-rules, symbol-discovery, api, ui, notifications]
last_updated: 2026-04-23
---

# Alert Rules — Symbol Auto-Resolve

## Summary

Creating an alert rule should not depend on the user having previously “discovered” a symbol (e.g., by visiting a stock detail page). The API now resolves and persists symbol metadata during alert creation.

## Behavior

- Input `TickerSymbol` is normalized (trim + uppercase) before persistence.
- API calls stock profile lookup during `AlertRuleService.CreateAsync` to resolve/persist symbol metadata.
- If the symbol cannot be resolved, alert creation fails.

## Impacted Areas

- API: `InventoryManagementSystem/InventoryAlert.Api/Services/AlertRuleService.cs`
- Domain: `InventoryManagementSystem/InventoryAlert.Domain/Entities/Postgres/AlertRule.cs`
- Tests: `InventoryManagementSystem/InventoryAlert.UnitTests/Application/Services/AlertRuleServiceTests.cs`
- UI: `InventoryAlert.UI/src/app/stocks/[symbol]/page.tsx`, `InventoryAlert.UI/src/components/NotificationBell.tsx`
- Worker: `InventoryManagementSystem/InventoryAlert.Worker/Filters/HangfireJobLoggingFilter.cs`

## Verification

- Unit tests cover alert creation paths without pre-existing catalog state.
- Manual:
  - Create an alert for a symbol that is not yet in the catalog; verify it succeeds and the symbol appears in `StockListing`.
  - Verify navbar bell badge updates via `GET /api/v1/notifications/unread-count` (returns `int`).

