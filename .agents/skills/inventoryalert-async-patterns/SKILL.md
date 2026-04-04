---
name: inventoryalert-async-patterns
description: Provides strict heuristics and guidelines for asynchronous Task handling matching C# 12 and the Finnhub BackgroundService constraints. Use this skill when modifying threaded handlers, dealing with execution scopes, adding Background services, passing CancellationTokens, or whenever async concurrency optimizations apply to the project.
---

# InventoryAlert Async Patterns

Because `InventoryAlert.Api` relies on background tasks (`FinnhubPricesSyncWorker`) that operate simultaneously alongside Web API controllers, handling asynchronous states is extremely delicate.

## CancellationToken Propagation
We rigidly enforce complete propagation. Every single async method inside `Domain`, `Application`, and `Infrastructure` layers **must** take a `CancellationToken ct` as its absolute last parameter. 
When making remote calls (like inside `FinnhubClient`), pass this token forward so that system tearsdown instantly when Docker sends a `SIGTERM`.

## BackgroundService Scope Safety
`BackgroundService` instances live as Singletons. Do not attempt to inject `Scoped` services (like `AppDbContext`, `Repositories`, or `IUnitOfWork`) inside the constructor of the Background Worker.

Please refer to the following code pattern architectures for implementing and debugging:

- **Factory Spawning**: See [references/scope-factory.md](references/scope-factory.md) for the acceptable `IServiceScopeFactory` paradigm to resolve dependencies inside Timers.
- **Task Await Anti-Patterns**: See [references/await-rules.md](references/await-rules.md) for handling the `.Result` or CS1998 exceptions when integrating `Task` flows.
