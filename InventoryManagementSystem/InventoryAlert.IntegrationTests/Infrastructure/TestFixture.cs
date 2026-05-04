using System.Data.Common;
using InventoryAlert.Api.Configuration;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Respawn;
using WireMock.Server;

namespace InventoryAlert.IntegrationTests.Infrastructure;

public class TestFixture : IAsyncLifetime
{
    public IConfiguration Configuration { get; private set; } = null!;
    public IServiceProvider ServiceProvider { get; private set; } = null!;
    public TestLoggerProvider LoggerProvider { get; } = new();
    public SeqLogReader LogReader { get; private set; } = null!;
    public WireMockServer WireMock { get; private set; } = null!;
    public ActionTestConfig ApiActionConfig { get; private set; } = null!;

    private Respawner _respawner = null!;
    private DbConnection _dbConnection = null!;

    public async Task InitializeAsync()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.test.json")
            .AddEnvironmentVariables()
            .Build();

        var settings = Configuration.Get<ApiSettings>() ?? new ApiSettings();

        // ── DI Setup ──
        ServiceProvider = SetupDI.BuildApiServiceProvider(Configuration, LoggerProvider);

        // ── Seq Log Reader Setup ──
        var seqUrl = Configuration["Seq:ServerUrl"] ?? "http://localhost:5341";
        LogReader = new SeqLogReader(seqUrl);

        // ── WireMock Setup ──
        WireMock = WireMockServer.Start();
        Configuration["Finnhub:ApiBaseUrl"] = WireMock.Urls[0];

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
        using var scope = ServiceProvider.CreateScope();
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

    public async Task DisposeAsync()
    {
        if (_dbConnection != null) await _dbConnection.DisposeAsync();
        if (WireMock != null) WireMock.Stop();
    }
}
