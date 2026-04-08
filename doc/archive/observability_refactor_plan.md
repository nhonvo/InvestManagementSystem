# Implementation Plan: Centralized Observability with Seq

This plan replaces ELK with **Seq**, focusing on a high-performance, developer-friendly logging experience optimized for the .NET ecosystem. We will implement end-to-end trace correlation from the initial UI/API request through business logic, background jobs, and database interactions.

## 1. Core Principles
- **Structured-First**: No string interpolation in logs; use property templates (`"User {UserId} logged in"`) for indexing.
- **Trace Continuity**: A single `CorrelationId` must bridge the gap between HTTP requests and asynchronous SQS/Hangfire jobs.
- **Contextual Enrichment**: Logs should "know" where they are (Service, Environment, Version) without manual boilerplate.

---

## 2. Infrastructure: The Seq Stack
We will use a lightweight, single-node Seq instance:
- **Seq Server**: ingestion via HTTP/CLEF (Compact Log Event Format).
- **Serilog.Sinks.Seq**: Native .NET sink for efficient, buffered shipping.
- **Docker Integration**: Seq runs as a container in `docker-compose.yml`.

---

## 3. The "Whole Flow" Logging Checklist

### 🟢 Layer 1: The Request (Web/API)
- [ ] **Correlation Middleware**: Generate or extract `X-Correlation-ID` from headers.
- [ ] **LogContext Enrichment**: Use `Serilog.Context.LogContext.PushProperty("CorrelationId", cid)` at the start of every request.
- [ ] **Request Logging**: Log the incoming path, method, and User ID (from JWT).
- [ ] **Global Error Capture**: Middleware must capture unhandled exceptions and log them with `Log.Error(ex, "Request failed")`.

### 🟡 Layer 2: The Method (Application/Services)
- [ ] **Method Parameters**: Log critical entry points with structured arguments (e.g., `Log.Debug("Processing alert for {Symbol}", symbol)`).
- [ ] **Performance Timings**: (Optional) Use `SerilogTimings` for high-value business operations.
- [ ] **Business Events**: Log result outcomes (Success/Failure/Ignored) rather than just "Method Finished".

### 🟠 Layer 3: The Job (Worker/Hangfire)
- [ ] **Context Propagation**: The `EventEnvelope` must carry the `CorrelationId`.
- [ ] **Job Scoping**: The Worker `SqsDispatcher` must re-hydrate the `LogContext` using the ID from the envelope before calling the processor.
- [ ] **Retry Tracking**: Log the `ReceiveCount` to track problematic messages in Seq.

### 🔵 Layer 4: The Database (Infrastructure/EF Core)
- [ ] **EF Core Interceptors**: Configure `LogTo(Log.Information)` or an `ItemInterceptor` to tag DB commands with the current `CorrelationId`.
- [ ] **Transaction Boundary**: Log when transactions begin and commit/roll back.

---

## 4. Implementation Details

### 4.1 Serilog Global Configuration
Centralized initialization in `Program.cs`:
```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "InventoryAlert.Api")
    .WriteTo.Seq(settings.Seq.ServerUrl, apiKey: settings.Seq.ApiKey)
    .WriteTo.Console()
    .CreateLogger();
```

### 4.2 Middleware Implementation (API)
```csharp
public class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            context.Response.Headers.Append("X-Correlation-ID", correlationId);
            await next(context);
        }
    }
}
```

---

## 5. Execution Phases

| Phase | Task | Details |
| :--- | :--- | :--- |
| **Phase 1** | **Seq Setup** | Add Seq to `docker-compose.yml` (Port 5341). |
| **Phase 2** | **Base Instrumentation** | Install `Serilog.Sinks.Seq` and implement `CorrelationIdMiddleware` in Api. |
| **Phase 3** | **Worker Re-hydration** | Update `SqsDispatcher` to push `CorrelationId` from envelope to `LogContext`. |
| **Phase 4** | **EF Core Tagging** | Configure DB logging to include correlation context in SQL commands. |

---

## 6. Expected Outcome
> *"In Seq, you filter by `CorrelationId == 'abc-123'`. You see the HTTP POST `/api/market-alert`, followed by the Service log, the SQS Publish log, and finally the Worker log executing the price check—all unified in one timeline."*
