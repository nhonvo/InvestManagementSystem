# Implementation Plan: Road to Perfect 100/100

> **Current Score:** 85/100 — **Target:** 100/100  
> **Missing:** Versioning (5 pts), Filtering/Ordering (10 pts), HATEOAS (5 pts), Caching (5 pts)

---

## Execution Order

```
Phase 1 (Versioning) → Phase 2 (Filter/Sort) → Phase 3 (HATEOAS) → Phase 4 (Cache)
```

- **Phase 1 first** — all other phases depend on the final route shape (URLs must be stable before HATEOAS links are built).
- **Phase 2 before Phase 3** — HATEOAS links embed query params, so the filter DTO must exist first.
- **Phase 4 last** — caching is purely additive, no structural dependencies.

---

## Phase 1 — API Versioning (5 pts)

**Goal:** Route all endpoints under `/api/v1/` using URL-path versioning.

### Files to Change

| File | Change |
|---|---|
| `Directory.Packages.props` | Add `Asp.Versioning.Mvc` + `Asp.Versioning.Mvc.ApiExplorer` to centralized package versions |
| `InventoryAlert.Api.csproj` | Reference those packages |
| `Web/Extensions/MvcExtension.cs` | Wire `AddApiVersioning()` + `AddMvc()` into `SetupMvc()` |
| `Web/Controllers/ProductsController.cs` | Add `[ApiVersion("1.0")]` + update route to `api/v{version:apiVersion}/[controller]` |
| `Web/Extensions/SwaggerExtension.cs` | Enumerate versions in Swagger document setup |

### Design Decisions

- **URL path versioning** (`/v1/`) over header versioning — judges can test in Swagger without custom headers.
- **Default version = 1.0** with `AssumeDefaultVersionWhenUnspecified = true` — keeps existing tests passing.
- `ReportApiVersions = true` adds `api-supported-versions` header to every response.

### Key Code Shape

```csharp
// Web/Extensions/MvcExtension.cs
services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.ReportApiVersions = true;
    opt.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddMvc();

// Web/Controllers/ProductsController.cs
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize]
public class ProductsController(...) : ControllerBase
```

---

## Phase 2 — Filtering & Ordering (10 pts)

**Goal:** `GET /api/v1/Products?name=Widget&minStock=5&sortBy=price_desc`

### Files to Change

| File | Change |
|---|---|
| `Application/DTOs/ProductQueryParams.cs` | **New file** — replaces `PaginationParams` on the list endpoint |
| `Domain/Interfaces/IProductRepository.cs` | Add overload: `GetPagedAsync(ProductQueryParams q, CancellationToken ct)` |
| `Infrastructure/Repositories/ProductRepository.cs` | Implement EF Core LINQ filter + `OrderBy` switch |
| `Application/Interfaces/IProductService.cs` | Update `GetProductsPagedAsync` signature to accept `ProductQueryParams` |
| `Application/Services/ProductService.cs` | Pass `ProductQueryParams` through to repository |
| `Web/Controllers/ProductsController.cs` | Accept `[FromQuery] ProductQueryParams` instead of `PaginationParams` |

> [!IMPORTANT]
> `IGenericRepository<T>` contract is **not changed** — the overload goes on `IProductRepository` only, respecting DDD layer boundaries.

### New DTO: `ProductQueryParams`

```csharp
// Application/DTOs/ProductQueryParams.cs
public class ProductQueryParams
{
    private const int MaxPageSize = 50;
    public int PageNumber { get; set; } = 1;

    private int _pageSize = 10;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    // Filtering
    public string? Name { get; set; }     // contains, case-insensitive
    public int? MinStock { get; set; }
    public int? MaxStock { get; set; }

    // Ordering: "name_asc" | "name_desc" | "price_asc" | "price_desc" | "stock_asc" | "stock_desc"
    public string? SortBy { get; set; }
}
```

### Infrastructure LINQ Strategy (ProductRepository)

```csharp
IQueryable<Product> query = _context.Products.AsNoTracking();

if (!string.IsNullOrWhiteSpace(q.Name))
    query = query.Where(p => p.Name.Contains(q.Name));

if (q.MinStock.HasValue) query = query.Where(p => p.StockCount >= q.MinStock.Value);
if (q.MaxStock.HasValue) query = query.Where(p => p.StockCount <= q.MaxStock.Value);

query = q.SortBy?.ToLowerInvariant() switch
{
    "name_desc"  => query.OrderByDescending(p => p.Name),
    "price_asc"  => query.OrderBy(p => p.CurrentPrice),
    "price_desc" => query.OrderByDescending(p => p.CurrentPrice),
    "stock_asc"  => query.OrderBy(p => p.StockCount),
    "stock_desc" => query.OrderByDescending(p => p.StockCount),
    _            => query.OrderBy(p => p.Name)   // default
};

var skip = (q.PageNumber - 1) * q.PageSize;
var total = await query.CountAsync(ct);
var items = await query.Skip(skip).Take(q.PageSize).ToListAsync(ct);
return (items, total);
```

---

## Phase 3 — HATEOAS / Hypermedia Links (5 pts)

**Goal:** Responses include navigational links (`self`, `next`, `prev`) in the paged list envelope.

### Files to Change

| File | Change |
|---|---|
| `Application/DTOs/HateoasLink.cs` | **New file** — `record HateoasLink(string Href, string Rel, string Method)` |
| `Application/DTOs/PagedResult.cs` | Add `List<HateoasLink> Links { get; set; } = []` |
| `Web/Controllers/ProductsController.cs` | Populate `Links` after service call using `Url.Action(...)` |

### New DTO: `HateoasLink`

```csharp
// Application/DTOs/HateoasLink.cs
public record HateoasLink(string Href, string Rel, string Method);
```

### Updated `PagedResult<T>`

```csharp
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int TotalItems { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
    public List<HateoasLink> Links { get; set; } = [];   // ← new
}
```

### Controller Link Population (ProductsController.GetProducts)

```csharp
pagedResult.Links.Add(new HateoasLink(
    Href: Url.Action(nameof(GetProducts), new { query.PageNumber, query.PageSize })!,
    Rel: "self", Method: "GET"));

if (pagedResult.PageNumber < pagedResult.TotalPages)
    pagedResult.Links.Add(new HateoasLink(
        Href: Url.Action(nameof(GetProducts), new { PageNumber = query.PageNumber + 1, query.PageSize })!,
        Rel: "next", Method: "GET"));

if (pagedResult.PageNumber > 1)
    pagedResult.Links.Add(new HateoasLink(
        Href: Url.Action(nameof(GetProducts), new { PageNumber = query.PageNumber - 1, query.PageSize })!,
        Rel: "prev", Method: "GET"));
```

### Expected Response Shape

```json
{
  "items": [...],
  "totalItems": 42,
  "pageNumber": 2,
  "pageSize": 10,
  "totalPages": 5,
  "links": [
    { "href": "/api/v1/Products?pageNumber=2&pageSize=10", "rel": "self",  "method": "GET" },
    { "href": "/api/v1/Products?pageNumber=3&pageSize=10", "rel": "next",  "method": "GET" },
    { "href": "/api/v1/Products?pageNumber=1&pageSize=10", "rel": "prev",  "method": "GET" }
  ]
}
```

---

## Phase 4 — HTTP Cache Headers (5 pts)

**Goal:** `GET` endpoints return `Cache-Control` headers so clients/CDNs can cache responses.

### Files to Change

| File | Change |
|---|---|
| `Program.cs` | Register `services.AddResponseCaching()` + `app.UseResponseCaching()` |
| `Web/Controllers/ProductsController.cs` | Add `[ResponseCache]` to `GetProducts` and `GetProductById` |

### Cache Strategy Per Endpoint

| Endpoint | Duration | Vary By | Rationale |
|---|---|---|---|
| `GET /Products` (list) | 30 s | `*` all query keys | Short TTL due to volatile stock; vary-by-query caches each filter combo separately |
| `GET /Products/{id}` | 60 s | `Accept` header | Single items are more stable; 60 s balances freshness vs. load |

### Key Code Shape

```csharp
// Program.cs
builder.Services.AddResponseCaching();
// Pipeline — MUST be BEFORE UseAuthentication
app.UseResponseCaching();

// ProductsController.cs
[HttpGet]
[ResponseCache(Duration = 30, VaryByQueryKeys = new[] { "*" })]
public async Task<IActionResult> GetProducts([FromQuery] ProductQueryParams query, ...)

[HttpGet("{id:int}")]
[ResponseCache(Duration = 60, VaryByHeader = "Accept")]
public async Task<IActionResult> GetProductById([FromRoute] int id, ...)
```

> [!WARNING]
> `UseResponseCaching()` must be placed **before** `UseAuthentication()` in the middleware pipeline. For `[Authorize]` endpoints, response caching will automatically use `Cache-Control: private` to prevent cross-user data leakage.

---

## Test Coverage Plan

| Phase | Happy Path | Edge Case |
|---|---|---|
| 1 – Versioning | `GET /api/v1/Products` → 200 | `GET /api/v99/Products` → 400 UnsupportedApiVersion |
| 2 – Filter | `?name=Widget` returns only matching rows | Empty filters return all (default) |
| 2 – Sort | `?sortBy=price_desc` returns descending order | Unknown `sortBy` falls back to `name_asc` default |
| 3 – HATEOAS | Page 2 body includes `next` and `prev` links | Last page: no `next` link present |
| 4 – Cache | `GET /Products` response has `Cache-Control: max-age=30` | Mutating `POST`/`PUT` returns `no-cache` |
