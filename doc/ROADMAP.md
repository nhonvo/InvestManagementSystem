# 🗺️ Development Roadmap

> Consolidated from all `TODO` comments across the codebase.
> Each task preserves the original TODO content and is grouped by theme, ordered by priority.

---

## 🔴 P0 — Critical / Blocking (Do First)

### 1. Docker & Containerization ✅

> **Source:** `ProductController.cs:L96` — *important high priority*

~~Setup docker and containerization for the application.~~ **✅ Complete.** See `SETUP_COMMANDS.md` and `EVENT_DRIVEN_PLAN.md` for full implementation details.

- [x] Create `Dockerfile` (multi-stage build)
- [x] Create `docker-compose.yml` (api + postgres + redis + moto + moto-init + worker)
- [x] Externalize config via `appsettings.Docker.json` (not inline env vars)
- [x] Moto init script (`SolutionFolder/moto-init/init-sqs.sh`) auto-creates SNS/SQS on boot

### 2. Global Exception Handling & Error Responses

> **Source:** `ProductController.cs:L79,L89,L91`

Implement a global exception handling mechanism (middleware or filters) to catch unhandled exceptions and return consistent, user-friendly JSON error responses. Use custom exceptions (`NotFoundException`, `ValidationException`) to represent different error types. Return appropriate HTTP status codes (400, 404, 500) with error codes. Ensure sensitive information is not exposed. Keep it simple and easy to understand for junior developers.

- [ ] Create custom exception classes (`NotFoundException`, `ValidationException`)
- [ ] Implement `ExceptionHandlingMiddleware`
- [ ] Standardize error response format (`{ code, message, details }`)

### 3. Logging Strategy

> **Source:** `ProductController.cs:L81,L91`

Implement comprehensive logging using Serilog or NLog. Log important events (product creation, updates, deletions) and errors with appropriate log levels (Information, Warning, Error). Include contextual information (product ID, user ID). Configure multiple sinks (console, file, database).

> **Note:** `FinnhubSyncWorker` logging should be addressed here but the worker itself is planned for **retirement** once `SyncPricesJob` in `InventoryAlert.Worker` is live (see `EVENT_DRIVEN_PLAN.md` — Phase C).

- [ ] Install and configure Serilog
- [ ] Add structured logging to `ProductServices`
- [ ] Add logging to `FinnhubSyncWorker` (temporary — will be removed with worker migration)

---

## 🟠 P1 — Architecture & Code Quality

### 4. Thin Controller / Service Layer Refactoring

> **Source:** `ProductController.cs:L59`

All the logic should be in the service layer. Move remaining logic to service layer and make the controller thin and simple, just for handling HTTP request and response.

**Current violation — `UpdateProductStockCount` (L49-L57):**
The PATCH endpoint fetches the product, mutates it, then calls update — this logic belongs in the service.

- [ ] Create `UpdateStockCountAsync(int id, int stockCount)` in `IProductService`
- [ ] Move PATCH logic from controller to service
- [ ] Controller should only call service and return result

### 5. RESTful API Design Review

> **Source:** `ProductController.cs:L30,L49,L72,L85,L86,L87,L88`

Review the design of the API endpoints to follow RESTful principles more closely:

| Current Issue | Fix |
|---|---|
| `HttpGet("{id}")` uses `[FromQuery]` but route has `{id}` | Use `[FromRoute]` or remove route param |
| `HttpPatch` has no route param for ID | Change to `HttpPatch("{id}")` |
| `HttpDelete` has no route param for ID | Change to `HttpDelete("{id}")` |
| `CreateProduct` returns `200 OK` | Return `201 Created` |
| `GetProductsByIds` name is misleading (single ID) | Rename to `GetProductById` |
| Method/endpoint naming inconsistency | Use verbs for actions + nouns for resources |

- [ ] Fix route parameter binding (`[FromRoute]` vs `[FromQuery]`)
- [ ] Return correct HTTP status codes (`201`, `204`, `404`)
- [ ] Rename methods to accurately reflect behavior
- [ ] Consider renaming `Product` entity to `InventoryProduct` or `StockProduct`

### 6. Unit of Work Pattern Review

> **Source:** `UnitOfWork.cs:L5`, `ProductServices.cs:L62`

Review the UnitOfWork pattern. Current concern: "I just want to reach rollback when scaling up with more tables. If fail when inserting one table, rollback the whole thing. Should we keep UnitOfWork or use a simpler implementation?"

**Verdict:** Keep UnitOfWork. It provides transaction safety for multi-table operations. For single-table operations, it adds minimal overhead since EF Core's `SaveChanges` is already atomic.

- [ ] Audit all service methods for consistent UnitOfWork usage
- [ ] Ensure `ExecuteTransactionAsync` wraps only multi-step operations
- [ ] Document when to use `UnitOfWork` vs plain `SaveChangesAsync`

### 7. Program.cs Organization

> **Source:** `Program.cs:L15,L34`

Review and enhance the `Program.cs` design. Use extension methods to organize the code and make it more readable and maintainable. Move static file names to constants.

- [ ] Create `ServiceCollectionExtensions.AddInfrastructure()`
- [ ] Create `ServiceCollectionExtensions.AddApplication()`
- [ ] Extract magic strings to a `Constants` class

---

## 🟡 P2 — Data Integrity & Validation

### 8. Input Validation

> **Source:** `ProductController.cs:L71,L87`

Add validation for input data using FluentValidation or Data Annotations. Ensure data being processed is valid and provide meaningful error messages when validation fails. Review use of data annotations or validation attributes on DTOs.

- [ ] Install `FluentValidation.AspNetCore`
- [ ] Create `ProductRequestDtoValidator`
- [ ] Register validators in DI
- [ ] Return `400 Bad Request` with validation details

### 9. DTO Design Review

> **Source:** `ProductController.cs:L74,L76`, `ProductServices.cs:L40`

Review `ProductDto` and `ProductRequestDto`. Separate the properties required for creating vs updating. Rename `GetHighValueProducts` to something more descriptive like `GetSignificantLossProducts`. The update method should use the ID from the route, not the body.

**Current state:** `ProductRequestDto` (create/update) and `ProductDto` (response) are already separated. ✅

- [ ] Verify `UpdateProductAsync` always uses route ID (not body ID)
- [ ] Rename `GetHighValueProducts` → `GetSignificantLossProducts`
- [ ] Rename endpoint `low-stock` → `price-alerts` or `loss-warnings`

### 10. Null Handling in Finnhub Responses

> **Source:** `ProductServices.cs:L107`

Should we check if the price is null before assigning it to `CurrentPrice`? Or should we set the current price to 0 if the price is null?

**Recommendation:** Skip the product entirely if the price is null. Setting it to 0 would trigger false loss alerts.

- [ ] Change `CurrentPrice = price.CurrentPrice ?? 0` to use `continue` when null
- [ ] Add logging when a symbol returns null price

### 11. BulkInsert Response Design

> **Source:** `ProductServices.cs:L71`

Should we return the list of created products or just the count?

**Recommendation:** Return `204 No Content` or the count. Returning thousands of full objects wastes bandwidth.

- [ ] Return `204 No Content` from controller
- [ ] Optionally return `{ count: N }` if the client needs feedback

---

## 🟢 P3 — Performance & Scalability

### 12. Caching

> **Source:** `ProductController.cs:L68`

Implement caching for frequently accessed data (product details) using in-memory caching or Redis, depending on scale and expected load.

- [ ] Add `IMemoryCache` for `GetProductById`
- [ ] Add cache invalidation on Update/Delete
- [ ] Evaluate Redis if scaling beyond a single instance

### 13. Pagination

> **Source:** `ProductController.cs:L73`

Implement pagination for list endpoints (`GetProducts`). Accept query parameters for page number and page size. Return subset of data with metadata (total items, total pages).

- [ ] Create `PaginationParams` (PageNumber, PageSize)
- [ ] Create `PagedResult<T>` wrapper
- [ ] Update `GetAllAsync` in repository to support `Skip/Take`

### 14. Finnhub Sync Design Review

> **Source:** `ProductServices.cs:L89`

Should we get the quote for each product on every API call, or store the price in the database and update it regularly?

**Current state:** ✅ Already implemented! `SyncCurrentPricesAsync` updates DB via BackgroundWorker. `GetHighValueProducts` now reads from DB + live Finnhub data.

- [ ] Consider making `GetHighValueProducts` read only from DB (no live API calls) for faster response
- [ ] Let the BackgroundWorker be the single source of truth for `CurrentPrice`

---

## 🔵 P4 — Security

### 15. Authentication & Authorization

> **Source:** `ProductController.cs:L69`

Add simple JWT-based authentication to secure the API endpoints and ensure only authorized users can access certain resources.

- [ ] Install `Microsoft.AspNetCore.Authentication.JwtBearer`
- [ ] Configure JWT in `Program.cs`
- [ ] Add `[Authorize]` to protected endpoints
- [ ] Create login/token endpoint

### 16. Web Security (OWASP)

> **Source:** `ProductController.cs:L84`

Protect against SQL injection, XSS, and CSRF. Use parameterized queries (EF Core handles this ✅), validate/sanitize input data, enforce HTTPS.

- [ ] Enable HTTPS redirection (`app.UseHttpsRedirection()`)
- [ ] Add CORS policy
- [ ] Review EF Core queries for raw SQL (ensure parameterized)
- [ ] Add rate limiting for Finnhub-related endpoints

---

## 🟣 P5 — Testing & Quality

### 17. Unit Tests

> **Source:** `ProductController.cs:L77`, `UnitTest1.cs:L3`

Implement unit tests for `ProductController` and `ProductService` using xUnit. Include tests for successful and error scenarios.

- [ ] Test `ProductServices.GetHighValueProducts` (loss math)
- [ ] Test `ProductServices.SyncCurrentPricesAsync` (null handling)
- [ ] Test `ProductController` status codes (200, 201, 404)
- [ ] Mock `IFinnhubClient` and `IProductRepository`

### 18. Async/Await Audit

> **Source:** `ProductController.cs:L70,L78`

Review all async/await usage. Ensure all operations are properly awaited. Review `CancellationToken` consistency across all layers.

- [ ] Add `CancellationToken` to `DeleteProductAsync`
- [ ] Ensure `CancellationToken` flows: Controller → Service → Repository
- [ ] Remove `CancellationToken.None` usages where a real token is available

### 19. Dependency Injection Review

> **Source:** `ProductController.cs:L92`

Review DI usage. Ensure constructor injection is used consistently. Review service lifetimes (transient, scoped, singleton) for correctness.

**Current state:**
| Service | Lifetime | Correct? |
|---|---|---|
| `RestClient` | via HttpClientFactory | ✅ |
| `IFinnhubClient` | Scoped | ✅ |
| `IProductService` | Scoped | ✅ |
| `IProductRepository` | Scoped | ✅ |
| `FinnhubSyncWorker` | Singleton (hosted) | ⚠️ Must use `IServiceScopeFactory` |

- [ ] Verify `FinnhubSyncWorker` uses `IServiceScopeFactory` (not direct injection)
- [ ] Document DI registration decisions

---

## ⚫ P6 — DevOps & Automation

### 20. Messaging & Advanced Background Jobs

> **Source:** `ProductController.cs:L97` — *important high priority*

See `EVENT_DRIVEN_PLAN.md` for full architecture. Infrastructure scaffolding is complete:

- [x] Evaluate Hangfire vs current `BackgroundService` → **Hangfire chosen**
- [x] Design message schema for price alerts → `EventEnvelope` + payload records in `Contracts`
- [x] `InventoryAlert.Worker` project created + NuGet packages installed
- [x] Docker moto-init: SNS topic + SQS queues auto-created on boot
- [ ] Implement `IEventPublisher` + `SnsEventPublisher` in Api (Phase B)
- [ ] Implement `PollSqsJob`, `SyncPricesJob`, and event handlers in Worker (Phase C–D)

### 21. CI/CD Pipeline

> **Source:** `ProductController.cs:L93`

Setup CI/CD using GitHub Actions or Azure DevOps. Automate build, test, and deployment. Include unit tests and integration tests in the pipeline.

- [ ] Create `.github/workflows/ci.yml`
- [ ] Steps: Restore → Build → Test → (optional) Docker push
- [ ] Add badge to README

### 22. API Documentation (Swagger)

> **Source:** `ProductController.cs:L83`

Enhance Swagger/OpenAPI documentation with XML comments, request/response examples, status codes, and endpoint descriptions.

- [ ] Enable XML documentation in `.csproj`
- [ ] Add `/// <summary>` to all controller methods
- [ ] Add `[ProducesResponseType]` attributes
- [ ] Add example requests/responses

### 23. AutoMapper Integration

> **Source:** `ProductController.cs:L75`

Implement AutoMapper to centralize mapping between domain models and DTOs. Reduce boilerplate in services.

- [ ] Install `AutoMapper.Extensions.Microsoft.DependencyInjection`
- [ ] Create `MappingProfile` with `Product ↔ ProductDto` maps
- [ ] Replace manual `MapProductToProductDto` methods

---

> **Last Updated:** 2026-04-04
>
> **Legend:** 🔴 P0 = Do now | 🟠 P1 = Next sprint | 🟡 P2 = Soon | 🟢 P3 = When needed | 🔵 P4 = Important but not urgent | 🟣 P5 = Quality | ⚫ P6 = Future
