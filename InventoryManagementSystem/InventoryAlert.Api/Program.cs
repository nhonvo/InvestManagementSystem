using InventoryAlert.Api.Configuration;
using InventoryAlert.Api.Extensions;
using InventoryAlert.Api.Middleware;
using InventoryAlert.Api.ServiceExtensions;
using InventoryAlert.Domain.Configuration;
using InventoryAlert.Infrastructure.Persistence.Postgres;
using InventoryAlert.Infrastructure.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Serilog;

// ─── Early Configuration Binding for Bootstrap ───────────────────────────────
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
    .AddEnvironmentVariables()
    .Build();

var settings = configuration.Get<ApiSettings>()
    ?? throw new InvalidOperationException("AppSettings configuration is missing.");

// ─── Serilog bootstrap ────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ApplyBaseConfiguration(settings, "InventoryAlert.Api")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ─── Serilog ──────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ApplyBaseConfiguration(settings, "InventoryAlert.Api")
            .ReadFrom.Services(services)
            .Enrich.With(services.GetRequiredService<CorrelationIdEnricher>());
    });

    // ─── DI Registrations ─────────────────────────────────────────────────────
    builder.Services.AddSingleton(settings);
    builder.Services.AddSingleton<AppSettings>(settings);
    builder.Services.AddCorrelationEnricher();
    builder.Services.AddHttpContextAccessor();
    
    builder.Services.AddTransient<GlobalExceptionMiddleware>();
    builder.Services.AddTransient<PerformanceMiddleware>();
    builder.Services.AddTransient<ApiBodyLoggingMiddleware>();
    builder.Services.AddTransient<CorrelationIdMiddleware>();

    // ─── Security / Auth / CORS ───────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Docker"))
        {
            options.AddPolicy("AllowAll",
                policy => policy
                    .SetIsOriginAllowed(origin => true)
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

    builder.Services.AddSignalR()
        .AddStackExchangeRedis(settings.Redis.ConnectionString, options => {
            options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("InventoryAlert_SignalR");
        });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerOpenAPI(settings);
    builder.Services.SetupMvc();
    builder.Services.AddCompressionCustom();
    builder.Services.SetupHealthCheck(settings);
    builder.Services.AddResponseCaching();
    builder.Services.AddWebApiInfrastructure(settings);

    var app = builder.Build();

    // ─── Auto-migrate ─────────────────────────────────────────────────────────
    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
    {
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    app.Logger.LogWarning("Database migration failed. Retry {RetryCount} in {RetryDelaySeconds}s.", retryCount, timeSpan.TotalSeconds);
                });

        await retryPolicy.ExecuteAsync(async () =>
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();
            await InventoryAlert.Infrastructure.Persistence.Postgres.DatabaseSeeder.SeedAsync(dbContext, app.Logger);
        });
    }

    // ─── Pipeline ─────────────────────────────────────────────────────────────
    app.UseMiddleware<CorrelationIdMiddleware>();
    
    // 1. Auth first to populate User
    app.UseAuthentication();
    app.UseAuthorization();

    // 2. Compression must be OUTER to loggers
    app.UseResponseCompression();
    
    // 3. Loggers capture raw JSON
    app.UseMiddleware<PerformanceMiddleware>();
    app.UseMiddleware<ApiBodyLoggingMiddleware>();
    
    // 4. GlobalException innermost to catch controller errors
    app.UseMiddleware<GlobalExceptionMiddleware>();

    app.UseStaticFiles();
    app.UseRouting();
    app.UseCors("AllowAll");
    app.UseResponseCaching();

    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
    {
        app.UseSwaggerWithUI();
    }

    app.ConfigureHealthCheck();
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
