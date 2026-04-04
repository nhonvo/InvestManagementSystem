# Unit Test Plan — InventoryAlert.Api

> **Stack**: xUnit · Moq · FluentAssertions (add) · Microsoft.AspNetCore.Mvc.Testing (already present)
> **Target**: net10.0 | **Test project**: `InventoryAlert.Tests`

---

## 🔍 Code Review Findings (Pre-Test)

| # | File | Issue | Severity |
|---|------|--------|----------|
| 1 | `ProductService.UpdateProductAsync` | `StockAlertThreshold` on `ProductRequest` is never mapped to the entity (entity has no such field). Silent data loss. | 🟠 Medium |
| 2 | `ProductService.GetPriceLossAlertsAsync` | `priceDelta = (origin - current) / origin` — does not guard against price increase direction. Logic is fragile for edge cases. | 🟡 Low |
| 3 | `ProductService.UpdateProductAsync` | `Product updated = new()` declared before the lambda; if transaction throws, `MapToResponse` runs on a blank entity. | 🟠 Medium |
| 4 | `ProductService.DeleteProductAsync` | Same blank-entity capture pattern as bug #3. | 🟠 Medium |
| 5 | `ProductsController.UpdateStockCount` | Builds a full `ProductRequest` inside the controller. Business logic leaking into the Web layer (SRP violation). | 🟡 Low |
| 6 | `FinnhubClient.GetQuoteAsync` | Error logged via `Console.WriteLine` — should use `ILogger<FinnhubClient>`. | 🟡 Low |
| 7 | `GenericRepository.DeleteAsync` | `async Task<T>` with no `await` inside — CS1998 warning. | 🟠 Medium |
| 8 | `GenericRepository.UpdateAsync` | Same CS1998. | 🟠 Medium |
| 9 | `GenericRepository.UpdateRangeAsync` | Same CS1998. | 🟠 Medium |

---

## 📁 Test Project Structure (to create)

```
InventoryAlert.Tests/
├── Application/
│   └── Services/
│       └── ProductServiceTests.cs          ← primary SUT
├── Web/
│   └── Controllers/
│       └── ProductsControllerTests.cs
├── Infrastructure/
│   └── Persistence/
│       └── Repositories/
│           └── GenericRepositoryTests.cs
└── Helpers/
    └── ProductFixtures.cs                  ← shared builders
```

---

## 1. `ProductServiceTests.cs`

**SUT**: `ProductService` | **Layer**: Application

### Mocks
```csharp
Mock<IUnitOfWork>         _unitOfWork     = new();
Mock<IProductRepository>  _productRepo    = new();
Mock<IFinnhubClient>      _finnhubClient  = new();

// SUT wired in constructor
_sut = new ProductService(_unitOfWork.Object, _productRepo.Object, _finnhubClient.Object);
```

---

### 1.1 `GetAllProductsAsync`

| Test Name | Arrange | Assert |
|-----------|---------|--------|
| `GetAll_ReturnsEmptyList_WhenNoProductsExist` | Repo returns `[]` | Result is empty |
| `GetAll_ReturnsMappedResponses_WhenProductsExist` | Repo returns 2 Products with known values | 2 items; Id/Name/TickerSymbol/OriginPrice/CurrentPrice/StockCount/PriceAlertThreshold all mapped |

---

### 1.2 `GetProductByIdAsync`

| Test Name | Arrange | Assert |
|-----------|---------|--------|
| `GetById_ReturnsNull_WhenProductNotFound` | Repo returns `null` for id=99 | `null` |
| `GetById_ReturnsMappedResponse_WhenProductFound` | Repo returns Product id=1 | Non-null; fields match |

---

### 1.3 `CreateProductAsync`

| Test Name | Arrange | Assert |
|-----------|---------|--------|
| `Create_CallsAddAsync_AndSaveChanges` | `AddAsync` returns entity; `SaveChangesAsync` completes | Both called exactly once |
| `Create_ReturnsMappedResponse_WithCorrectFields` | Known request values | Response matches request fields |
| `Create_MapsNullTickerSymbol_ToEmptyString` | `TickerSymbol = null` in request | Entity passed to `AddAsync` has `TickerSymbol == ""` |

---

### 1.4 `UpdateProductAsync`

| Test Name | Arrange | Assert |
|-----------|---------|--------|
| `Update_ThrowsKeyNotFoundException_WhenProductNotFound` | Repo returns `null` for id=99 | `KeyNotFoundException` with "99" in message |
| `Update_UpdatesAllFields_WhenProductFound` | Repo returns existing; transaction invokes delegate | Entity has Name/TickerSymbol/StockCount/CurrentPrice/OriginPrice/PriceAlertThreshold updated |
| `Update_ExecutesInsideTransaction` | same | `ExecuteTransactionAsync(Func<Task>, ct)` called once |
| `Update_ReturnsUpdatedProduct_NotBlankDefault` *(Bug #3)* | `UpdateAsync` mock returns patched entity | Response.Id ≠ 0 |

---

### 1.5 `DeleteProductAsync`

| Test Name | Arrange | Assert |
|-----------|---------|--------|
| `Delete_ReturnsNull_WhenProductNotFound` | Repo returns `null` | `null`; `ExecuteTransactionAsync` never called |
| `Delete_CallsDeleteAsync_InsideTransaction_WhenFound` | Repo returns product; delegate invoked | `DeleteAsync` once; transaction once |
| `Delete_ReturnsMappedResponse_WhenFound` | same | Response maps the deleted product |

---

### 1.6 `BulkInsertProductsAsync`

| Test Name | Arrange | Assert |
|-----------|---------|--------|
| `BulkInsert_CallsAddRangeAsync_InsideTransaction` | Transaction invokes delegate | `AddRangeAsync` called with 3 entities |
| `BulkInsert_MapsAllRequests_ToEntities` | Capture arg on `AddRangeAsync` | Each entity has correct Name, Ticker, OriginPrice |

---

### 1.7 `GetPriceLossAlertsAsync`

| Test Name | Arrange | Assert |
|-----------|---------|--------|
| `PriceLoss_ReturnsEmpty_WhenNoProducts` | Repo `[]` | Empty |
| `PriceLoss_SkipsProduct_WhenFinnhubReturnsNull` | 1 product; Finnhub → `null` | Empty |
| `PriceLoss_SkipsProduct_WhenCurrentPriceIsZero` | Finnhub → `{ CurrentPrice = 0 }` | Empty |
| `PriceLoss_ReturnsAlert_WhenDropExceedsThreshold` | OriginPrice=100, Threshold=0.1; Quote=85 (15% drop > 10%) | 1 alert; `PriceChangePercent=0.15`; `PriceDiff=-15` |
| `PriceLoss_DoesNotAlert_WhenDropBelowThreshold` | OriginPrice=100, Threshold=0.2; Quote=90 (10% < 20%) | Empty |
| `PriceLoss_AlertsMapsAllFields_Correctly` | Above scenario | All 8 fields on `PriceLossResponse` match expected |

---

### 1.8 `SyncCurrentPricesAsync`

| Test Name | Arrange | Assert |
|-----------|---------|--------|
| `Sync_SkipsProduct_WhenFinnhubReturnsNull` | Product CurrentPrice=50; Finnhub `null` | `CurrentPrice` still 50 |
| `Sync_UpdatesCurrentPrice_WhenValidQuote` | Finnhub → `CurrentPrice=75` | Product entity mutated to 75 before `UpdateRangeAsync` |
| `Sync_CallsUpdateRangeAsync_InsideTransaction` | same | `UpdateRangeAsync` called once inside transaction |

---

## 2. `ProductsControllerTests.cs`

**SUT**: `ProductsController` | **Layer**: Web (pure unit, no HTTP pipeline)

### Mocks
```csharp
Mock<IProductService> _service = new();
_sut = new ProductsController(_service.Object);
```

### 2.1 `GetProducts`
| Test | Assert |
|------|--------|
| `GetProducts_Returns200_WithList` | `OkObjectResult` with 2 items |

### 2.2 `GetProductById`
| Test | Assert |
|------|--------|
| `GetById_Returns200_WhenFound` | `OkObjectResult` |
| `GetById_Returns404_WhenNotFound` | `NotFoundResult` |

### 2.3 `CreateProduct`
| Test | Assert |
|------|--------|
| `Create_Returns201CreatedAtAction` | `CreatedAtActionResult`; ActionName="GetProductById"; RouteValues["id"]=5 |

### 2.4 `UpdateProduct`
| Test | Assert |
|------|--------|
| `Update_Returns200_WhenSuccessful` | `OkObjectResult` |
| `Update_Propagates_KeyNotFoundException` | Exception propagates unswallowed |

### 2.5 `UpdateStockCount`
| Test | Assert |
|------|--------|
| `UpdateStock_Returns404_WhenProductNotFound` | `NotFoundResult`; `UpdateProductAsync` never called |
| `UpdateStock_CallsUpdate_WithNewStockCount` | `UpdateProductAsync` called with `StockCount==10`; other fields from existing product preserved |
| `UpdateStock_Returns200_WhenSuccessful` | `OkObjectResult` |

### 2.6 `DeleteProduct`
| Test | Assert |
|------|--------|
| `Delete_Returns204_WhenDeleted` | `NoContentResult` |
| `Delete_Returns404_WhenNotFound` | `NotFoundResult` |

### 2.7 `BulkInsertProducts`
| Test | Assert |
|------|--------|
| `BulkInsert_Returns204_Always` | `NoContentResult` |

### 2.8 `GetPriceLossAlerts`
| Test | Assert |
|------|--------|
| `PriceAlerts_Returns200_WithAlerts` | `OkObjectResult` with 2 items |

---

## 3. `GenericRepositoryTests.cs`

**SUT**: `GenericRepository<Product>` | **Layer**: Infrastructure  
**Requires**: `Microsoft.EntityFrameworkCore.InMemory`

```csharp
AppDbContext CreateContext() =>
    new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
```

| Test | Assert |
|------|--------|
| `AddAsync_PersistsEntity_AndReturnsIt` | Entity in DB; returned entity matches values |
| `GetByIdAsync_ReturnsEntity_WhenExists` | Correct product returned |
| `GetByIdAsync_ReturnsNull_WhenNotExists` | `null` |
| `GetAllAsync_ReturnsAllEntities` | 3 items returned |
| `UpdateAsync_MutatesEntity` | DB reflects new Name after SaveChanges |
| `DeleteAsync_RemovesEntity` | Entity gone after SaveChanges |
| `AddRangeAsync_AddsAllEntities` | 3 entities persisted |

---

## 4. `ProductFixtures.cs` — Shared Builders

```csharp
public static class ProductFixtures
{
    public static Product BuildProduct(
        int id = 1, string name = "Test", string ticker = "TST",
        decimal originPrice = 100m, decimal currentPrice = 90m,
        double threshold = 0.2, int stock = 10) => new()
    {
        Id = id, Name = name, TickerSymbol = ticker,
        OriginPrice = originPrice, CurrentPrice = currentPrice,
        PriceAlertThreshold = threshold, StockCount = stock
    };

    public static ProductRequest BuildRequest(
        string name = "Test", string ticker = "TST",
        decimal originPrice = 100m, decimal currentPrice = 90m,
        double threshold = 0.2, int stock = 10) => new()
    {
        Name = name, TickerSymbol = ticker,
        OriginPrice = originPrice, CurrentPrice = currentPrice,
        PriceAlertThreshold = threshold, StockCount = stock
    };

    public static FinnhubQuoteResponse BuildQuote(decimal currentPrice = 90m) =>
        new() { CurrentPrice = currentPrice };
}
```

---

## 5. NuGet Packages to Add

```xml
<!-- InventoryAlert.Tests.csproj -->
<PackageReference Include="FluentAssertions" Version="6.12.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.0" />
```

> Already present: `Moq`, `xunit`, `Microsoft.AspNetCore.Mvc.Testing`, `coverlet.collector`

---

## 6. Implementation Priority

| Priority | Test Class | Reason |
|----------|-----------|--------|
| 🔴 P0 | `ProductServiceTests` | Core business logic; highest risk area |
| 🟠 P1 | `ProductsControllerTests` | HTTP contract; catches routing & binding issues |
| 🟡 P2 | `GenericRepositoryTests` | EF Core integration safety net |

---

## 7. Coverage Targets

| Layer | Line Coverage |
|-------|--------------|
| Application/Services | ≥ 90% |
| Web/Controllers | ≥ 85% |
| Infrastructure/Repositories | ≥ 80% |
| Infrastructure/Workers | Integration test scope — out of this plan |
