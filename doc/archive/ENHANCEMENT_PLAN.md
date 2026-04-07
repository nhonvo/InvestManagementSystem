# Enhancement & Tracking Plan

> **Last Updated:** 2026-04-05
> Use this as a living checklist. Items marked ✅ are implemented and verified.

---

## 🏁 Critical Improvements

### 1. Architectural Alignment (DDD)

- [x] **Thin Controllers:** Alert filter logic moved to `ProductService`. Controller is now thin. ✅
- [x] **Repository & UoW Refactoring:** `UnitOfWork` uses DI-injected repositories. ✅
- [x] **Consolidate Services:** All logic in `Application/Services`. Top-level `Services/` folder removed. ✅
- [x] **Interface Naming:** `ProductService` (singular) matches `IProductService`. ✅

### 2. Design Patterns to Implement

- [ ] **CQRS (Command Query Responsibility Segregation):** Use **MediatR** to separate read and write requests.
    - *Why:* Simplifies code as it grows, making handlers more focused.
- [ ] **Domain Events:** Dispatch events (e.g., `LowStockDetectedEvent`) from the Domain entity.
    - *Why:* Decouples product updates from notification logic.
- [ ] **Static Factory / Builder Pattern:** Use for creating `Product` entities with complex state.
- [ ] **Specification Pattern:** For building reusable filters in `GetPriceLossAlertsAsync`.
    - *Why:* Keeps query logic out of the Domain and into reusable objects.

### 3. Code Quality & Patterns

- [x] **Mapping:** Manual mapping centralized in static `MapToResponse` / `MapToEntity` helpers. ✅
- [x] **CancellationToken Consistency:** `CancellationToken ct` passed through all layers. ✅
- [x] **Async/Await Fix:** `.Result` usage eliminated — all methods use `await`. ✅
- [x] **REST Best Practices:** ✅
    - [x] `PATCH /{id}/stock` endpoint uses `[FromRoute]` binding.
    - [x] `GetProductById` correctly named with `[FromRoute] int id`.

### 4. Missing Features (Challenge Requirements)

- [x] **Alert Logic:** `LastAlertSentAt` 1-hour cooldown implemented in `GetPriceLossAlertsAsync`. No spam alerts. ✅
- [x] **Error Handling:** `ExceptionHandlingMiddleware` implemented — RFC-7807 ProblemDetails, all domain exceptions mapped. ✅

---

## 🛠️ Performance & Security (Planned)

- [ ] **Caching:** Implement `IDistributedCache` (Redis) or `IMemoryCache` for `GetProductById` to reduce DB load.
- [ ] **Authentication:** Add JWT Bearer authentication to protect the `POST/PUT/DELETE` endpoints.
- [x] **Validation:** `FluentValidation` registered for `ProductRequest`. Returns `400 Bad Request` with details. ✅

---

## 📝 Code Review Summary

| File | Issue | Status |
| :--- | :--- | :--- |
| `ProductController.cs` | Logic in Controller | ✅ Fixed — thin controller, all logic in service |
| `ProductServices.cs` | `.Result` usage | ✅ Fixed — all `await` |
| `UnitOfWork.cs` | Violation of DI | ✅ Fixed — repositories injected |
| `GenericRepository.cs` | Shadowing `T` | ✅ Fixed |
| `Program.cs` | Massive file | ✅ Fixed — `AddInfrastructure()` + `AddApplicationServices()` extensions |
