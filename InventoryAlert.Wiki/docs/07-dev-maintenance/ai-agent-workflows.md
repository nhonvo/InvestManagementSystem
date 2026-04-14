# AI Agent Workflows

> Catalog of repository-specific patterns and available agent slash commands.

## Available Workflows

| Command | Purpose |
|---|---|
| `/add-entity` | Scaffold a new domain entity end-to-end (Domain → Infra → Api → Worker) |
| `/add-feature` | Add new behavior to an existing entity |
| `/feature-flow` | Full end-to-end development flow from requirement to merged code |
| `/db-migration` | Create and apply an EF Core database migration |
| `/run-tests` | Run unit tests with optional coverage report |
| `/code-review` | Pre-merge checklist for code quality |
| `/doc` | Scaffold and sync documentation |
| `/plan` | Design feature + freeze context (FSD spec) |
| `/search` | BM25 search across codebase (token saver) |

---

## Key Patterns to Know

### Transaction Capture Pattern

```csharp
// ✅ GOOD — result assigned inside the lambda
PortfolioPositionResponse result = null!; // null-forgiving acceptable: assigned before read
await _unitOfWork.ExecuteTransactionAsync(async () => {
    var updated = await _repo.UpdateAsync(entity);
    result = MapToResponse(updated);
}, ct);
return result;
```

### Primary Constructor Injection (C# 12)

```csharp
public class PortfolioService(IUnitOfWork unitOfWork, IStockDataService stockData)
    : IPortfolioService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IStockDataService _stockData = stockData;

    public async Task<PortfolioPositionResponse> OpenPositionAsync(
        CreatePositionRequest request, string userId, CancellationToken ct)
    {
        var listing = await _unitOfWork.StockListings.FindBySymbolAsync(request.TickerSymbol, ct)
            ?? throw new InvalidOperationException($"Symbol {request.TickerSymbol} must be resolved first.");

        PortfolioPositionResponse result = null!;
        await _unitOfWork.ExecuteTransactionAsync(async () => {
            var trade = new Trade { UserId = Guid.Parse(userId), ... };
            await _unitOfWork.Trades.AddAsync(trade, ct);
            result = MapToResponse(trade, listing);
        }, ct);
        return result;
    }
}
```

### Finnhub Null Guard

```csharp
// Always null-check before using Finnhub data
var quote = await _finnhub.GetQuoteAsync(symbol, ct);
if (quote?.CurrentPrice is null or 0)
{
    _logger.LogWarning("[SyncPrices] {Symbol} returned zero price. Skipping.", symbol);
    return;
}
```

### Read-Only EF Queries

```csharp
// All read-only queries must use AsNoTracking()
var listings = await _db.StockListings
    .AsNoTracking()
    .Where(s => s.IsActive)
    .ToListAsync(ct);
```

---

## File Placement Quick Reference

| What you're creating | Where it goes |
|---|---|
| New entity | `Domain/Entities/Postgres/` |
| New DTO | `Domain/DTOs/` |
| New repository interface | `Domain/Interfaces/` |
| New validator | `Domain/Validators/` |
| EF Core config | `Infrastructure/Persistence/Configurations/` |
| Repository impl | `Infrastructure/Persistence/Repositories/` |
| External client | `Infrastructure/External/` |
| New controller | `Api/Controllers/` |
| New API service | `Api/Services/` |
| New scheduled job | `Worker/ScheduledJobs/` |
| New SQS handler | `Worker/IntegrationEvents/Handlers/` |
