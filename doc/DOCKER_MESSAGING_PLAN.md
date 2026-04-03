# 🐳 Docker + Messaging Implementation Plan

> Combined from `DOCKER_PLAN.md` and ROADMAP tasks P0-1 and P6-20.
> This is your hands-on step-by-step guide to implement both features.

---

## 📦 Part 1: Docker & Containerization

### Step 1 — `.dockerignore`

Create at `InventoryManagementSystem/.dockerignore`:

```
**/bin/
**/obj/
**/.git/
**/appsettings.Development.json
**/*.user
```

**Why:** Keeps the Docker build context small and prevents dev secrets from leaking into the image.

---

### Step 2 — `Dockerfile` (Multi-stage build)

Create at `InventoryManagementSystem/Dockerfile`:

```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "InventoryAlert.Api/InventoryAlert.Api.csproj"
RUN dotnet publish "InventoryAlert.Api/InventoryAlert.Api.csproj" \
    -c Release -o /app/publish

# Stage 2: Runtime (small image)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

# Security: run as non-root user
RUN adduser --disabled-password --gecos '' appuser
USER appuser

WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "InventoryAlert.Api.dll"]
```

---

### Step 3 — `docker-compose.yml`

Create at `InventoryManagementSystem/docker-compose.yml`:

```yaml
services:

  api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Database__DefaultConnection=host=db;port=5432;Database=InventoryAlertDb;Username=postgres;Password=${DB_PASSWORD}
      - Finnhub__ApiKey=${FINNHUB_API_KEY}
      - Finnhub__ApiBaseUrl=https://finnhub.io/api/v1/
      - MinuteSyncCurrentPrice=10
    depends_on:
      db:
        condition: service_healthy   # Wait for DB to be ready!
    networks:
      - app-network

  db:
    image: postgres:17-alpine
    environment:
      POSTGRES_DB: InventoryAlertDb
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - pgdata:/var/lib/postgresql/data   # Persist data across restarts
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      retries: 5
    networks:
      - app-network

volumes:
  pgdata:

networks:
  app-network:
```

---

### Step 4 — `.env` file (NOT committed to git)

Create at `InventoryManagementSystem/.env`:

```env
DB_PASSWORD=yourStrongPassword123!
FINNHUB_API_KEY=d77d58hr01qp6afl79qgd77d58hr01qp6afl79r0
```

Add `.env` to your `.gitignore`:
```
.env
```

---

### Step 5 — Update `appsettings.json`

Your `appsettings.json` (not Development) should use environment variable fallbacks:

```json
{
  "Database": {
    "DefaultConnection": ""
  },
  "Finnhub": {
    "ApiBaseUrl": "",
    "ApiKey": ""
  },
  "MinuteSyncCurrentPrice": 10
}
```

> The real values come from Docker environment variables at runtime.
> In Development, `appsettings.Development.json` (gitignored) overrides with localhost values.

---

### Step 6 — EF Core Migrations on Startup

In `Program.cs`, apply migrations automatically when the app starts in a container:

```csharp
// After app.Build()
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate(); // Auto-apply pending migrations
}
```

---

### ✅ Docker Checklist

- [x] Create `.dockerignore`
- [x] Create `Dockerfile` (multi-stage, non-root user)
- [x] Create `docker-compose.yml` (api + db with healthcheck, `ASPNETCORE_ENVIRONMENT=Docker`)
- [x] Create `.env.example` template (safe to commit)
- [x] Create `appsettings.json` (schema only, empty values — overridden by env vars at runtime)
- [x] Create `appsettings.Docker.json` (hostname `db`, reads env vars)
- [x] Create `appsettings.Example.json` (onboarding template for new devs, no real secrets)
- [ ] Create `.env` from `.env.example` and fill real secrets (run: `Copy-Item .env.example .env`)
- [ ] Add `.env` to `.gitignore`
- [ ] Add `db.Database.Migrate()` to `Program.cs`
- [ ] Test: `docker-compose up --build`
- [ ] Verify: API reachable at `http://localhost:8080/swagger`

---

---

## 📨 Part 2: Messaging & Advanced Background Jobs

> Current state: You have a `FinnhubSyncWorker` using `BackgroundService` + `PeriodicTimer`.
> Goal: Evolve this into a more robust, scalable messaging pipeline.

### Decision: Hangfire vs BackgroundService

| Feature | `BackgroundService` (Current) | Hangfire |
|---|---|---|
| Dashboard UI | ❌ None | ✅ `/hangfire` |
| Retry on failure | ❌ Manual | ✅ Built-in |
| Job history / logs | ❌ Just console logs | ✅ Persistent DB |
| Recurring schedule | ⚠️ PeriodicTimer | ✅ Cron expressions |
| Complexity | ✅ Simple | ⚠️ Needs DB table |

**Recommendation:** Keep `BackgroundService` for now. Add Hangfire only when you need the Dashboard or retry management.

---

### Step 1 — Evaluate Hangfire (Optional Upgrade)

Install:
```bash
dotnet add package Hangfire.AspNetCore
dotnet add package Hangfire.PostgreSql
```

Register in `Program.cs`:
```csharp
builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(connectionString));
builder.Services.AddHangfireServer();

// Replace BackgroundService with a Hangfire recurring job:
RecurringJob.AddOrUpdate<IProductService>(
    "sync-prices",
    service => service.SyncCurrentPricesAsync(CancellationToken.None),
    Cron.Minutely * 10); // every 10 minutes
```

---

### Step 2 — Alert Notifications (SNS Pattern)

Instead of just logging alerts, publish them to an **AWS SNS topic** or use a local **email/webhook** for now.

**Pragmatic approach — Interface first:**

```csharp
// Application/Interfaces/IAlertNotifier.cs
public interface IAlertNotifier
{
    Task NotifyAsync(ProductLossDto product, CancellationToken cancellationToken);
}
```

**Local implementation (for dev/testing):**
```csharp
// Infrastructure/Notifications/ConsoleAlertNotifier.cs
public class ConsoleAlertNotifier(ILogger<ConsoleAlertNotifier> logger) : IAlertNotifier
{
    public Task NotifyAsync(ProductLossDto product, CancellationToken ct)
    {
        logger.LogWarning(
            "🚨 ALERT: {Name} ({Symbol}) has dropped {Percent:P1}! CurrentPrice: ${Current}, OriginPrice: ${Origin}",
            product.Name, product.TickerSymbol,
            product.PriceChangePercent,
            product.CurrentPrice,
            product.OriginPrice);
        return Task.CompletedTask;
    }
}
```

**Production SNS implementation (register via DI based on environment):**
```csharp
// Infrastructure/Notifications/SnsAlertNotifier.cs
public class SnsAlertNotifier(IAmazonSimpleNotificationService sns, IConfiguration config) : IAlertNotifier
{
    public async Task NotifyAsync(ProductLossDto product, CancellationToken ct)
    {
        var message = $"{product.Name} ({product.TickerSymbol}) dropped {product.PriceChangePercent:P1}";
        await sns.PublishAsync(config["Aws:SnsTopicArn"], message, ct);
    }
}
```

---

### Step 3 — Update `FinnhubSyncWorker`

Wire the notifier into the worker:

```csharp
using (var scope = scopeFactory.CreateScope())
{
    var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
    var notifier = scope.ServiceProvider.GetRequiredService<IAlertNotifier>();

    // 1. Sync prices in DB
    await productService.SyncCurrentPricesAsync(stoppingToken);

    // 2. Check for losses
    var alerts = await productService.GetHighValueProducts(stoppingToken);

    // 3. Notify for each alert
    foreach (var alert in alerts)
    {
        await notifier.NotifyAsync(alert, stoppingToken);
    }
}
```

---

### Step 4 — Add to `docker-compose.yml` (SQS local emulation)

For local development, use **LocalStack** to emulate AWS SNS/SQS:

```yaml
  localstack:
    image: localstack/localstack
    ports:
      - "4566:4566"
    environment:
      - SERVICES=sns,sqs
    networks:
      - app-network
```

---

### ✅ Messaging Checklist

- [ ] Create `IAlertNotifier` interface in `Application/Interfaces/`
- [ ] Implement `ConsoleAlertNotifier` for dev
- [ ] Register `IAlertNotifier` in `Program.cs`
- [ ] Update `FinnhubSyncWorker` to call `notifier.NotifyAsync()`
- [ ] (Optional) Add Hangfire for Dashboard + retry support
- [ ] (Future) Implement `SnsAlertNotifier` for production
- [ ] (Future) Add LocalStack to docker-compose for local AWS emulation

---

> **Start with Docker first** — it will make all future testing (including messaging) consistent across machines.
