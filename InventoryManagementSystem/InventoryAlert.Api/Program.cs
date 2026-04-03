using InventoryAlert.Api.Infrastructure.Persistence;
using InventoryAlert.Api.Web.Configuration;
using InventoryAlert.Api.Web.ServiceExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ─── Configuration ────────────────────────────────────────────────────────────
var settings = builder.Configuration.Get<AppSettings>()
    ?? throw new InvalidOperationException("AppSettings configuration is missing.");

builder.Services.AddSingleton(settings);

// ─── API / Swagger ────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "🚀 Pragmatic Inventory Alert API",
        Version = "v1",
        Description = "Real-time stock monitoring and significant price loss detection system integrated with Finnhub API.",
        Contact = new OpenApiContact
        {
            Name = "OJT Training Team",
            Email = "dev@ojt-training.local"
        }
    });
});

builder.Services.AddControllers();

// ─── Layered DI registration ──────────────────────────────────────────────────
builder.Services
    .AddApplicationServices()
    .AddInfrastructure(settings);

// ─── Build ────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ─── Auto-migrate on startup ──────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// ─── Middleware Pipeline ──────────────────────────────────────────────────────
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "V1 Docs");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Pragmatic Inventory API Docs";
    });
}

app.UseAuthorization();
app.MapControllers();
app.Run();
