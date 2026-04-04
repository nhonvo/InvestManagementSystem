# Build Guide: Temporal Action Queue

This guide provides a detailed, step-by-step walkthrough to build the **Temporal Action Queue** project from scratch using .NET 9.

---

## Phase 1: Environment Setup

### 1. Initialize the Project
Create a new Web API project with Controllers.
```bash
dotnet new webapi -n JobQueue.Api -o . --use-controllers --no-https
```

### 2. Add Dependencies
Install the required Entity Framework Core packages for PostgreSQL.
```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
```


---

## Phase 2: Core Data Infrastructure

### 3. Create the Job Model
Create `Models/ScheduledJob.cs`. This entity defines what a task looks like in the database.
```csharp
namespace JobQueue.Api.Models;

public class ScheduledJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TaskName { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public bool IsProcessed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### 4. Setup the Database Context
Create `Data/AppDbContext.cs`. We use a **Primary Constructor** for the options.
```csharp
using Microsoft.EntityFrameworkCore;
using JobQueue.Api.Models;

namespace JobQueue.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ScheduledJob> Jobs => Set<ScheduledJob>();
}
```

---

## Phase 3: The Engine (Background Job)

### 5. Create the Worker Service
Create `Services/JobProcessorService.cs`.
**CRITICAL**: Background services are singletons. To use a scoped `DbContext`, you must manually create a scope using `IServiceProvider`.

```csharp
using JobQueue.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace JobQueue.Api.Services;

public class JobProcessorService(IServiceProvider serviceProvider, ILogger<JobProcessorService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Polling for due tasks...");

            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var dueJobs = await context.Jobs
                    .Where(j => !j.IsProcessed && j.ScheduledAt <= DateTime.UtcNow)
                    .ToListAsync(stoppingToken);

                foreach (var job in dueJobs)
                {
                    logger.LogInformation("Executing Job: {Name}", job.TaskName);
                    await Task.Delay(1000, stoppingToken); // Simulate work
                    job.IsProcessed = true;
                }

                if (dueJobs.Any()) await context.SaveChangesAsync(stoppingToken);
            }

            await Task.Delay(30000, stoppingToken); // Wait 30 seconds
        }
    }
}
```

---

## Phase 4: API & Registration

### 6. Create the Controller
Create `Controllers/JobsController.cs` to handle incoming requests.
```csharp
[ApiController]
[Route("api/[controller]")]
public class JobsController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetJobs() => 
        Ok(await context.Jobs.AsNoTracking().OrderByDescending(j => j.CreatedAt).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> CreateJob([FromBody] CreateJobRequest r)
    {
        var job = new ScheduledJob { TaskName = r.Name, ScheduledAt = r.ExecuteAt };
        context.Jobs.Add(job);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetJobs), new { id = job.Id }, job);
    }
}
public record CreateJobRequest(string Name, DateTime ExecuteAt);
```

### 7. Configure Dependency Injection
Update `Program.cs` to link everything together.
```csharp
builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseNpgsql("Host=localhost;Database=jobsdb;Username=postgres;Password=password"));

builder.Services.AddHostedService<JobProcessorService>();

// Enable Static Files for the Dashboard
app.UseDefaultFiles();
app.UseStaticFiles();
```

---

## Phase 5: The Interface (Dashboard)

### 8. Create the Frontend
1. Create a `wwwroot` folder.
2. Create `wwwroot/index.html` with a modern Glassmorphism theme (refer to the project files for the full HTML/CSS/JS).
3. The dashboard connects to `/api/jobs` via JavaScript `fetch`.

---

## Phase 6: Deployment & Runtime

### 9. Database Migrations
Initialize the database using the EF Core CLI tool.
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 10. Run the project
```bash
dotnet run
```
Your dashboard will be available at `http://localhost:5000`.

---

## Advanced Practice Goals
- **Status Enum**: Change `IsProcessed` to `Pending`, `Running`, `Completed`.
- **Error Handling**: What if the database connection fails in the worker? Wrap the scope in a `try-catch`.
- **Cancellation**: Add a `DELETE` endpoint to cancel scheduled tasks.
