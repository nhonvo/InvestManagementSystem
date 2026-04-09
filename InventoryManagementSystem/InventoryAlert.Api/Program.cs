using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Web.Configuration;
using InventoryAlert.Api.Web.Extensions;
using InventoryAlert.Api.Web.Middleware;
using InventoryAlert.Api.Web.ServiceExtensions;
using InventoryAlert.Contracts.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;

// ─── Early Configuration Binding for Bootstrap ───────────────────────────────
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
    .AddEnvironmentVariables()
    .Build();

var bootstrapSettings = configuration.Get<AppSettings>()
    ?? throw new InvalidOperationException("AppSettings configuration is missing.");

// ─── Serilog bootstrap ────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq(bootstrapSettings.Seq.ServerUrl)
    .WriteTo.File("logs/inventoryalert-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ─── Serilog ──────────────────────────────────────────────────────────────
    builder.Host.UseSerilog();

    // ─── Clean Configuration Injection ────────────────────────────────────────
    // Re-bind to ensure consistency with the container's environment
    var settings = builder.Configuration.Get<AppSettings>() ?? bootstrapSettings;

    builder.Services.AddSingleton(settings);
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICorrelationProvider, InventoryAlert.Api.Web.Infrastructure.CorrelationProvider>();
    builder.Services.AddTransient<GlobalExceptionMiddleware>();
    builder.Services.AddTransient<PerformanceMiddleware>();
    builder.Services.AddTransient<CorrelationIdMiddleware>();

    // ─── Security / Auth / CORS ───────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        // Dev: allow all origins for local tooling. Production: restrict to configured origins.
        if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Docker"))
        {
            options.AddPolicy("AllowAll",
                policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
        }
        else
        {
            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
            options.AddPolicy("AllowAll",
                policy => policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
        }
    });

    var jwtKey = settings.Jwt.Key;
    if (string.IsNullOrEmpty(jwtKey))
    {
        if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Docker"))
        {
            jwtKey = "InventoryAlert_Temporary_Default_Key_For_Dev_Only_1234567890";
            Log.Warning("Using temporary default JWT key. NOT FOR PRODUCTION.");
        }
        else
        {
            throw new InvalidOperationException("Jwt:Key is required in configuration.");
        }
    }

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = false,
                ValidateAudience = false
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var authHeader = context.Request.Headers["Authorization"].ToString();
                    if (string.IsNullOrEmpty(authHeader)) return Task.CompletedTask;

                    // If the header starts with 'ey' (JWT start) and doesn't have 'Bearer ', 
                    // extract it manually to improve DX for manual curl/tool calls.
                    if (authHeader.StartsWith("eyJ", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Token = authHeader;
                    }
                    return Task.CompletedTask;
                }
            };
        });
    builder.Services.AddAuthorization();

    // ─── API / Core Services ──────────────────────────────────────────────────
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerOpenAPI(settings);
    builder.Services.SetupMvc();
    builder.Services.AddCompressionCustom();
    builder.Services.SetupHealthCheck(settings);

    builder.Services.AddHealthChecks();
    builder.Services.AddResponseCaching();

    builder.Services
        .AddApplicationServices()
        .AddInfrastructure(settings);


    // ─── Build ────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ─── Auto-migrate on startup (Dev/Docker only) ──────────────────────────
    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            app.Logger.LogInformation("Applying database migrations...");
            dbContext.Database.Migrate();
            app.Logger.LogInformation("Database migration complete.");

            await InventoryAlert.Api.Infrastructure.Persistence.DatabaseSeeder.SeedAsync(
                dbContext, app.Logger);
        }
        catch (Exception ex)
        {
            app.Logger.LogCritical(ex, "Error occurred during database migration");
            throw;
        }
    }

    // ─── Middleware Pipeline ──────────────────────────────────────────────────
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseMiddleware<PerformanceMiddleware>();

    app.UseResponseCompression();
    app.UseStaticFiles();                           // serves wwwroot/ (dashboard)

    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseCors("AllowAll");

    app.UseResponseCaching();

    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
    {
        app.UseSwaggerWithUI();
    }

    app.UseAuthentication();
    app.UseAuthorization();
    app.ConfigureHealthCheck();
    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
