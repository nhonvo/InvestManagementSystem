---
name: finnhub-integration
description: Provides guidelines for implementing, syncing, and testing Finnhub API integrations. Use this when extending the external price sync service, working on FinnhubPricesSyncWorker, or managing stock/ticker REST responses.
---

# Finnhub API Integration

This skill documents how to safely hit the external Finnhub market API without crashing the `InventoryAlert.Api` container.

## Finnhub Client Implementation

All external communication happens strictly within `Infrastructure/External/FinnhubClient.cs`. No service layer should perform HTTP directly.

### Defensive Integration Patterns
When querying Finnhub, we assume the API might fail, rate-limit, or return incomplete objects. 

- **No Throw Policy**: Do not throw an exception on 404s, timeouts, or invalid shapes. Return `null` to the Application layer log the failure gracefully.
- **ILogger usage**: Emitting errors to `ILogger<FinnhubClient>` is mandatory. Using `Console.WriteLine` is banned (Tech Debt #5 fix tracking).
- **Null Defaults**: Guard access properties. `if (quote?.CurrentPrice is null or 0) { ... }`

```csharp
// Example Safe Implementation
public async Task<FinnhubQuoteResponse?> GetQuoteAsync(string tickerSymbol, CancellationToken ct)
{
    try
    {
        var request = new RestRequest($"/quote?symbol={tickerSymbol}");
        var response = await _client.ExecuteAsync<FinnhubQuoteResponse>(request, ct);
        
        if (!response.IsSuccessful || response.Data == null)
        {
            _logger.LogWarning("Finnhub API returned failure for {Ticker}: {Message}", tickerSymbol, response.ErrorMessage);
            return null; // Safe fallback
        }
        
        return response.Data;
    }
    catch(Exception ex)
    {
        _logger.LogError(ex, "Unexpected exception calling Finnhub for {Ticker}", tickerSymbol);
        return null;
    }
}
```

## Background Sync (`FinnhubPriceSyncWorker`)

The worker uses a `PeriodicTimer` for non-blocking execution inside a `BackgroundService`.

**Critical rule**: BackgroundServices resolve as Singleton by default. `IUnitOfWork` and Repositories are Scoped. 
If modifying the worker, **you must use an `IServiceScopeFactory`** to resolve scoped dependencies safely, otherwise, an `InvalidOperationException` throws on startup.

```csharp
using var scope = _scopeFactory.CreateScope();
var productService = scope.ServiceProvider.GetRequiredService<IProductService>();

// Safely execute cycle...
```

For full endpoint mapping references available on the free tier, check the project docs: `doc/finnhub/FINNHUB_FREE_ENDPOINTS.md`.
