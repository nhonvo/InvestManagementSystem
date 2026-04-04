# Service Scope Factory Resolving

Any threaded execution not spawned from an HTTP Web Request (i.e., Background timers, external Event Subscribers) has no inherent request scope container.

```csharp
// Inside FinnhubPricesSyncWorker.ExecuteAsync()

using var scope = _scopeFactory.CreateScope();
var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
var productRepo = scope.ServiceProvider.GetRequiredService<IProductRepository>();

// Action can safely be executed inside the bounds of the "using" destruction scope
await unitOfWork.ExecuteTransactionAsync(async () => {
    // perform actions
}, stoppingToken);
```
