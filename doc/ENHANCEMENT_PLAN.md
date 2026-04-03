# Enhancement & Tracking Plan

This document tracks identified improvements, code smells, and `TODO` items across the **Automated Inventory Alert System**. Use this as a checklist for your practice sessions.

## 🏁 Critical Improvements

### 1. Architectural Alignment (DDD)
- [ ] **Thin Controllers:** Move the logic from `ProductController.GetProductsBelowAlertThreshold` (the `.Where()` filter) into the `ProductService`.
- [ ] **Repository & UoW Refactoring:** Refactor `UnitOfWork` to inject repositories instead of using the `new` keyword.
- [ ] **Consolidate Services:** You have both `InventoryAlert.Api/Services` and `InventoryAlert.Api/Application/Services`. Move all implementation logic to `Application/Services` and delete the top-level `Services` folder to follow DDD properly.
- [ ] **Interface Naming:** Rename `ProductServices.cs` implementation class to `ProductService` (singular) to match the interface `IProductService`.

### 2. Design Patterns to Implement
- [ ] **CQRS (Command Query Responsibility Segregation):** Use **MediatR** to separate read and write requests.
    - *Why:* Simplifies code as it grows, making handlers more focused.
- [ ] **Domain Events:** Dispatch events (e.g., `LowStockDetectedEvent`) from the Domain entity.
    - *Why:* Decouples product updates from notification logic.
- [ ] **Static Factory / Builder Pattern:** Use for creating `Product` entities with complex state.
- [ ] **Specification Pattern:** For building reusable filters in `GetProductsBelowAlertThreshold`.
    - *Why:* Keeps query logic out of the Domain and into reusable objects.

### 3. Code Quality & Patterns
- [ ] **Mapping:** Manual mapping is used in `ProductServices.cs`. Consider implementing **AutoMapper** or **Mapster** to reduce boilerplate.
- [ ] **CancellationToken Consistency:** Ensure `cancellationToken` is passed through all async calls (some methods currently use `CancellationToken.None`).
- [ ] **Async/Await Fix:** In `ProductServices.UpdateProductAsync`, `.Result` is being used (`_productRepository.UpdateAsync(product).Result`). This can cause deadlocks. Change to `await`.
- [ ] **REST Best Practices:**
    - [ ] `UpdateProductStockCount` should be a `PATCH` request using a JSON Patch document or a specific DTO.
    - [ ] `GetProductsByIds` currently uses `[FromQuery] int id` which is misleading (it fetches a single product, not multiple). Fix route/parameter naming.

### 3. Missing Features (The "Challenge" Requirements)
- [ ] **Alert Logic:** Implement the check for `LastAlertSentAt` in the notification logic to prevent log spam (only alert if `null` or > 1 hour ago).
- [ ] **Error Handling:** Add a Global Exception Middleware to handle errors gracefully and return standard RFC 7807 Problem Details.

---

## 🛠️ Performance & Security (Planned)
- [ ] **Caching:** Implement `IDistributedCache` (Redis) or `IMemoryCache` for `GetProductById` to reduce DB load.
- [ ] **Authentication:** Add JWT Bearer authentication to protect the `POST/PUT/DELETE` endpoints.
- [ ] **Validation:** Add **FluentValidation** for incoming `ProductDto` (e.g., Name shouldn't be empty, StockCount >= 0).

---

## 📝 Code Review Summary

| File | Issue | Suggestion |
| :--- | :--- | :--- |
| `ProductController.cs` | Logic in Controller | Move `Where` filter to `ProductService`. |
| `ProductServices.cs` | `.Result` usage | Never use `.Result` or `.Wait()`. Use `await`. |
| `UnitOfWork.cs` | Violation of DI | Don't use `new ProductRepository()`. Inject it. |
| `GenericRepository.cs` | Shadowing `T` | (Fixed in previous step) |
| `Program.cs` | Massive file | Use **Extension Methods** (e.g., `builder.Services.AddInfrastructure()`) to keep it clean. |
