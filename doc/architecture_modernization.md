# Architecture Modernization Track

## 🎯 Objectives
- Rename `InventoryAlert.Contracts` to `InventoryAlert.Domain` for architectural clarity.
* Consolidate Infrastructure by moving single-tenant logic into `Api` or `Worker`.
* Finalize the "Lean Profile" by removing redundant layers.

## 🏃 Progress
| Task | Status | Notes |
| :--- | :--- | :--- |
| **Project Renaming** | ✅ Completed | Renamed `Contracts` to `Domain`. |
| **Logic Scoping Audit** | ✅ Completed | Identified Infrastructure logic for relocation. |
| **Infrastructure Cleanup** | ✅ Completed | Moved scoped logic to `Api` or `Worker`. |
| **Namespace Refactoring** | ✅ Completed | All namespaces updated to `Domain` and specific service locations. |

## 🔍 Logic Scoping Audit (Draft)
| Component | Primary Usage | Recommendation |
| :--- | :--- | :--- |
| `AuthService` | API | ✅ Moved to `InventoryAlert.Api`. |
| `SqsHelper` | Worker | ✅ Moved to `InventoryAlert.Worker`. |
| `TelegramAlertNotifier` | Worker | ✅ Moved to `InventoryAlert.Worker`. |
| `StockDataService` | API | ✅ Moved to `InventoryAlert.Api`. |
| `ProductService` | API | ✅ Moved to `InventoryAlert.Api`. |
