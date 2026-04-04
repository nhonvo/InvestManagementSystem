# 🔬 DDD Refactoring Audit — Progress Tracker
## Naming Conventions, File Locations & Structure

---

## ✅ Step 1 — Create missing folders

| Folder | Status |
| :--- | :--- |
| `Domain/Interfaces/` | ✅ Created |
| `Domain/Events/` | ✅ Created |
| `Infrastructure/Persistence/Configurations/` | ✅ Created |
| `Web/Controllers/` | ✅ Created |
| `Web/Configuration/` | ✅ Created |
| `Web/Middleware/` | ⬜ Phase 2 (GlobalExceptionHandler) |

---

## ✅ Step 2 — Move files

| File | From | To | Status |
| :--- | :--- | :--- | :--- |
| `AppDbContext.cs` | `Infrastructure/` | `Infrastructure/Persistence/` | ✅ Done |
| `ProductConfiguration.cs` | `Infrastructure/Persistence/` | `Infrastructure/Persistence/Configurations/` | ✅ Done |
| `IGenericRepository.cs` | `Infrastructure/Persistence/Interfaces/` | `Domain/Interfaces/` | ✅ Done |
| `IProductRepository.cs` | `Infrastructure/Persistence/Interfaces/` | `Domain/Interfaces/` | ✅ Done |
| `IUnitOfWork.cs` | `Infrastructure/Persistence/Interfaces/` | `Domain/Interfaces/` | ✅ Done |
| `ProductController.cs` | `Controllers/` | `Web/Controllers/ProductsController.cs` | ✅ Done |
| `AppSettings.cs` | root | `Web/Configuration/` | ✅ Done |

---

## ✅ Step 3 — Rename files + classes

| Old Name | New Name | Status |
| :--- | :--- | :--- |
| `ProductServices.cs` | `ProductService.cs` | ✅ Done |
| `ProductDto.cs` | Split → `ProductRequest.cs` + `ProductResponse.cs` + `PriceLossResponse.cs` | ✅ Done |
| `FinnhubSyncWorker.cs` | `FinnhubPriceSyncWorker.cs` | ✅ Done |
| class `ProductServices` | `ProductService` | ✅ Done |
| class `ProductRequestDto` | `ProductRequest` | ✅ Done |
| class `ProductDto` | `ProductResponse` | ✅ Done |
| class `ProductLossDto` | `PriceLossResponse` | ✅ Done |
| class `Finnhub` (in AppSettings) | `FinnhubSetting` | ✅ Done |

---

## ✅ Step 4 — Rename methods + update namespaces

| Change | Status |
| :--- | :--- |
| `GetHighValueProducts` → `GetPriceLossAlertsAsync` | ✅ Done |
| `DeleteProductAsync(int)` → `DeleteProductAsync(int, CancellationToken)` | ✅ Done |
| Namespace `Infrastructure.Persistence.Interfaces` → `Domain.Interfaces` | ✅ Done |
| Namespace `Infrastructure` (AppDbContext) → `Infrastructure.Persistence` | ✅ Done |
| Namespace `InventoryAlert.Api` (AppSettings) → `Web.Configuration` | ✅ Done |
| Controller methods: proper `[FromRoute]`/`[FromBody]`/status codes | ✅ Done |

---

## ✅ Cleanup — Stub/placeholder files deleted

| Deleted Stub | Replaced By |
| :--- | :--- |
| `Infrastructure/AppDbContext.cs` | `Infrastructure/Persistence/AppDbContext.cs` ✅ |
| `Infrastructure/Persistence/ProductConfiguration.cs` | `Infrastructure/Persistence/Configurations/ProductConfiguration.cs` ✅ |
| `Infrastructure/Persistence/Interfaces/IGenericRepository.cs` | `Domain/Interfaces/IGenericRepository.cs` ✅ |
| `Infrastructure/Persistence/Interfaces/IProductRepository.cs` | `Domain/Interfaces/IProductRepository.cs` ✅ |
| `Infrastructure/Persistence/Interfaces/IUnitOfWork.cs` | `Domain/Interfaces/IUnitOfWork.cs` ✅ |
| `Application/Services/ProductServices.cs` | `Application/Services/ProductService.cs` ✅ |
| `Application/DTOs/ProductDto.cs` | `ProductRequest.cs` + `ProductResponse.cs` + `PriceLossResponse.cs` ✅ |
| `Controllers/ProductController.cs` | `Web/Controllers/ProductsController.cs` ✅ |
| `Infrastructure/Workers/FinnhubSyncWorker.cs` | `Infrastructure/Workers/FinnhubPriceSyncWorker.cs` ✅ |
| `AppSettings.cs` (root) | `Web/Configuration/AppSettings.cs` ✅ |
| `Infrastructure/Persistence/Interfaces/` (folder) | Deleted ✅ |
| `Controllers/` (folder) | Deleted ✅ |
| `Services/` (folder) | Deleted ✅ |

---

## ✅ Step 5 — Fix `Program.cs` + DI organization

| Task | Status |
| :--- | :--- |
| Extract DI into `Web/ServiceExtensions/ApplicationServiceExtensions.cs` | ✅ Done |
| Extract DI into `Web/ServiceExtensions/InfrastructureServiceExtensions.cs` | ✅ Done |
| `builder.Services.AddApplicationServices()` | ✅ Done |
| `builder.Services.AddInfrastructure(settings)` | ✅ Done |
| Fix `using Microsoft.OpenApi` (v2 namespace change from Swashbuckle v10) | ✅ Done |
| Fix `IFinnhubClient` return type → `Task<FinnhubQuoteResponse?>` (nullable) | ✅ Done |
| Fix `Product.Name/TickerSymbol` → `= string.Empty` (CS8618) | ✅ Done |
| Fix null-coalescing in `ProductService` mapper (CS8601) | ✅ Done |

---

## ✅ Build Verification

| Task | Status |
| :--- | :--- |
| `dotnet build` — 0 Errors, 0 Warnings | ✅ **PASSED** |

---

## ⬜ Phase 2 — Future Work (not in scope yet)

| Task | Notes |
| :--- | :--- |
| `Web/Middleware/GlobalExceptionHandler.cs` | Replace try/catch in controller with `IExceptionHandler` middleware |
| Add FluentValidation for `ProductRequest` | Validate `Name`, `OriginPrice > 0`, `PriceAlertThreshold` range |
| Move Swagger config into `Web/ServiceExtensions/` | `AddSwaggerDocumentation()` extension |

---

## 🧑‍💻 Final Folder Tree — Achieved ✅

```
InventoryAlert.Api/
├── Domain/
│   ├── Entities/           ✅ Product.cs
│   ├── Interfaces/         ✅ IGenericRepository, IProductRepository, IUnitOfWork
│   └── Events/             ✅ (placeholder)
├── Application/
│   ├── DTOs/               ✅ ProductRequest, ProductResponse, PriceLossResponse
│   ├── Interfaces/         ✅ IProductService
│   └── Services/           ✅ ProductService.cs
├── Infrastructure/
│   ├── Persistence/        ✅ AppDbContext.cs
│   │   ├── Configurations/ ✅ ProductConfiguration.cs
│   │   └── Repositories/   ✅ GenericRepository, ProductRepository, UnitOfWork
│   ├── External/           ✅ FinnhubClient + IFinnhubClient + Models
│   └── Workers/            ✅ FinnhubPriceSyncWorker.cs
└── Web/
    ├── Controllers/          ✅ ProductsController.cs
    ├── Configuration/        ✅ AppSettings.cs
    ├── ServiceExtensions/    ✅ ApplicationServiceExtensions, InfrastructureServiceExtensions
    ├── Middleware/           ⬜ Phase 2
    └── Program.cs            ✅ Clean bootstrap — 64 lines
```
