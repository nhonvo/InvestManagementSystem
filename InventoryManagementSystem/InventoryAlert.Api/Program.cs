using InventoryAlert.Api.Configuration;
using InventoryAlert.Api.Extensions;
using InventoryAlert.Api.Middleware;
using InventoryAlert.Api.ServiceExtensions;
using InventoryAlert.Domain.Configuration;
using InventoryAlert.Infrastructure.Persistence.Postgres;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Serilog;
using Serilog.Events;

// ─── Early Configuration Binding for Bootstrap ───────────────────────────────
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
    .AddEnvironmentVariables()
    .Build();


var bootstrapSettings = configuration.Get<ApiSettings>()
    ?? throw new InvalidOperationException("AppSettings configuration is missing.");


// ─── Serilog bootstrap ────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "InventoryAlert.Api")
    .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production")
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
    var settings = builder.Configuration.Get<ApiSettings>() ?? bootstrapSettings;

    builder.Services.AddSingleton(settings);
    builder.Services.AddSingleton<AppSettings>(settings);
    builder.Services.AddHttpContextAccessor();
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
                policy => policy
                    .SetIsOriginAllowed(origin => true) // Echoes back the origin instead of '*' to allow credentials
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
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
                    // SignalR Query String handling (Standard for WebSockets)
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    
                    if (!string.IsNullOrEmpty(accessToken) && 
                        path.StartsWithSegments(InventoryAlert.Domain.Interfaces.SignalRConstants.NotificationHubRoute))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });
    builder.Services.AddAuthorization();

    // ─── SignalR with Redis Backplane ─────────────────────────────────────────
    builder.Services.AddSignalR()
        .AddStackExchangeRedis(settings.Redis.ConnectionString, options => {
            options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("InventoryAlert_SignalR");
        });

    // ─── API / Core Services ──────────────────────────────────────────────────
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerOpenAPI(settings);
    builder.Services.SetupMvc();
    builder.Services.AddCompressionCustom();
    builder.Services.SetupHealthCheck(settings);
    builder.Services.AddResponseCaching();

    builder.Services
        .AddWebApiInfrastructure(settings);


    // ─── Build ────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ─── Auto-migrate on startup (Dev/Docker only) ──────────────────────────
    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
    {
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    app.Logger.LogWarning("Database migration failed. Retry {RetryCount} in {RetryDelaySeconds}s. Error: {ErrorMessage}", retryCount, timeSpan.TotalSeconds, exception.Message);
                });

        await retryPolicy.ExecuteAsync(async () =>
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            app.Logger.LogInformation("Applying database migrations...");
            await dbContext.Database.MigrateAsync();
            app.Logger.LogInformation("Database migration complete.");

            await InventoryAlert.Infrastructure.Persistence.Postgres.DatabaseSeeder.SeedAsync(
                dbContext, app.Logger);
        });
    }

    // ─── Middleware Pipeline ──────────────────────────────────────────────────
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<PerformanceMiddleware>();
    app.UseMiddleware<GlobalExceptionMiddleware>();

    app.UseResponseCompression();
    app.UseStaticFiles();                           // serves wwwroot/ (dashboard)

    if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Docker"))
    {
        app.UseHttpsRedirection();
    }
    app.UseRouting();
    app.UseCors("AllowAll");

    app.UseResponseCaching();

    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
    {
        app.UseSwaggerWithUI();
    }

    app.ConfigureHealthCheck();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHub<InventoryAlert.Infrastructure.Hubs.NotificationHub>(InventoryAlert.Domain.Interfaces.SignalRConstants.NotificationHubRoute);
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

