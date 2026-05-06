using System.Data.Common;
using InventoryAlert.Api.Configuration;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Infrastructure.Persistence.Postgres;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Respawn;
using WireMock.Server;
using InventoryAlert.Worker.ScheduledJobs;
using InventoryAlert.Worker.IntegrationEvents.Handlers;
using InventoryAlert.Worker.IntegrationEvents.Routing;
using InventoryAlert.Worker.Interfaces;
using InventoryAlert.Worker.Configuration;
using InventoryAlert.Worker.Utilities;
using Moq;
using Hangfire;
using Serilog;

namespace InventoryAlert.IntegrationTests.Infrastructure;

public class TestFixture : WebApplicationFactory<InventoryAlert.Api.Program>, IAsyncLifetime
{
    public IConfiguration Configuration { get; private set; } = null!;
    public override IServiceProvider Services => _factory?.Services ?? base.Services;
    public TestLoggerProvider LoggerProvider { get; } = new();
    public SeqLogReader LogReader { get; private set; } = null!;
    public WireMockServer WireMock { get; private set; } = null!;
    public ActionTestConfig ApiActionConfig { get; private set; } = null!;

    private Respawner _respawner = null!;
    private DbConnection _dbConnection = null!;
    private WebApplicationFactory<InventoryAlert.Api.Program> _factory = null!;

    public async Task InitializeAsync()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.test.json")
            .AddEnvironmentVariables()
            .Build();

        var settings = Configuration.Get<ApiSettings>() ?? new ApiSettings();

        // ── WireMock Setup ──
        WireMock = WireMockServer.Start();
        
        // ── Set Environment Variables for API Bootstrap ──
        Environment.SetEnvironmentVariable("Database__DefaultConnection", Configuration["Database:DefaultConnection"]);
        Environment.SetEnvironmentVariable("Redis__ConnectionString", Configuration["Redis:ConnectionString"]);
        Environment.SetEnvironmentVariable("Aws__EndpointUrl", Configuration["Aws:EndpointUrl"]);
        Environment.SetEnvironmentVariable("Aws__SnsTopicArn", Configuration["Aws:SnsTopicArn"]);
        Environment.SetEnvironmentVariable("Aws__SqsQueueUrl", Configuration["Aws:SqsQueueUrl"]);
        Environment.SetEnvironmentVariable("Aws__SqsDlqUrl", Configuration["Aws:SqsDlqUrl"]);
        Environment.SetEnvironmentVariable("Finnhub__ApiBaseUrl", WireMock.Urls[0]);
        Environment.SetEnvironmentVariable("Seq__ServerUrl", Configuration["Seq:ServerUrl"] ?? "http://localhost:5341");
        Environment.SetEnvironmentVariable("SKIP_SEEDING", "true");
        Environment.SetEnvironmentVariable("Jwt__Key", "InventoryAlert_Secure_Development_32_Char_Key");
        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "test");
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "test");
        Environment.SetEnvironmentVariable("AWS_DEFAULT_REGION", "us-east-1");

        // ── Factory Setup (In-Process API) ──
        _factory = this.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddLogging(lb => 
                {
                    lb.ClearProviders();
                    lb.AddProvider(LoggerProvider);
                });

                services.AddSingleton<ILoggerProvider>(LoggerProvider);
                
                // ── Mocks for Hangfire ──
                services.AddSingleton(new Mock<IBackgroundJobClient>().Object);
                services.AddSingleton(new Mock<IRecurringJobManager>().Object);

                // ── Worker Services for In-Process Job Testing ──
                var workerSettings = Configuration.Get<WorkerSettings>() ?? new WorkerSettings();
                services.AddSingleton(workerSettings);
                services.AddScoped<ISqsHelper, SqsHelper>();

                services.AddScoped<SyncPricesJob>();
                services.AddScoped<SyncMetricsJob>();
                services.AddScoped<SyncEarningsJob>();
                services.AddScoped<SyncRecommendationsJob>();
                services.AddScoped<SyncInsidersJob>();
                services.AddScoped<NewsSyncJob>();
                services.AddScoped<CleanupPriceHistoryJob>();
                services.AddScoped<IProcessQueueJob, ProcessQueueJob>();

                // Handlers
                services.AddScoped<MarketPriceAlertHandler>();
                services.AddScoped<LowHoldingsHandler>();
                services.AddScoped<IRawDefaultHandler, DefaultHandler>();
                services.AddScoped<IIntegrationMessageRouter, IntegrationMessageRouter>();
            });
        });

        // ── Seq Log Reader Setup ──
        var seqUrl = Configuration["Seq:ServerUrl"] ?? "http://localhost:5341";
        LogReader = new SeqLogReader(seqUrl);

        // ── Action Config for API tests ──
        ApiActionConfig = new ActionTestConfig(LogReader);

        // ── DB Cleanup Setup (Respawn) ──
        var connectionString = settings.Database.DefaultConnection;
        
        // Robust connection with retries (Postgres might be slow to initialize DBs)
        await ExecuteWithRetry(async () => 
        {
            if (_dbConnection != null) await _dbConnection.DisposeAsync();
            _dbConnection = new NpgsqlConnection(connectionString);
            await _dbConnection.OpenAsync();
        }, 5, TimeSpan.FromSeconds(2));
        
        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = new[] { "public" },
            TablesToIgnore = new[] { new Respawn.Graph.Table("__EFMigrationsHistory") }
        });
    }

    public HttpClient CreateTestClient() => _factory.CreateClient();

    private async Task ExecuteWithRetry(Func<Task> action, int maxRetries, TimeSpan delay)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                await action();
                return;
            }
            catch (Exception ex) when (i < maxRetries - 1)
            {
                Console.WriteLine($"[TestFixture] Connection attempt {i + 1} failed: {ex.Message}. Retrying in {delay.TotalSeconds}s...");
                await Task.Delay(delay);
            }
        }
    }

    public async Task ResetStateAsync()
    {
        await _respawner.ResetAsync(_dbConnection);
        WireMock.Reset();
        
        // ── Reset Redis ──
        using (var redisScope = Services.CreateScope())
        {
            var redis = redisScope.ServiceProvider.GetRequiredService<IRedisHelper>();
            await redis.FlushDatabaseAsync(CancellationToken.None);
        }

        // ── Reset Container WireMock (for Tier 2/3) ──
        var containerWireMockUrl = Configuration["WiremockSettings:BaseUrl"] ?? "http://localhost:9091";
        using var client = new HttpClient();
        try 
        {
            await client.PostAsync($"{containerWireMockUrl}/__admin/reset", null);
        }
        catch (Exception)
        {
            // Ignore if container wiremock is not available
        }

        // ── Seed Default Admin User (for legacy test compatibility) ──
        using var scope = Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var admin = new User
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Username = "admin",
            Email = "admin@inventoryalert.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = "Admin"
        };
        await uow.Users.AddAsync(admin, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);

        LoggerProvider.Clear();
    }

    public new async Task DisposeAsync()
    {
        if (_dbConnection != null) await _dbConnection.DisposeAsync();
        if (WireMock != null) WireMock.Stop();
        if (_factory != null) _factory.Dispose();
        await base.DisposeAsync();
    }
}
