using FluentAssertions;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Infrastructure.Persistence.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;
using InventoryAlert.IntegrationTests.Abstractions;
using InventoryAlert.IntegrationTests.Clients;
using InventoryAlert.IntegrationTests.Fixtures;
using InventoryAlert.Worker.ScheduledJobs;
using InventoryAlert.Worker.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RestSharp;
using Xunit.Abstractions;

namespace InventoryAlert.IntegrationTests.Tests.Worker;

public class SyncPricesJobTest : BaseIntegrationTest
{
    private readonly WiremockAdminClient _wiremockAdminClient;
    private readonly StockClient _stockClient;
    private readonly AuthClient _authClient;
    private readonly SyncPricesJob _job;
    private readonly IUnitOfWork _uow;
    private readonly IFinnhubClient _mockFinnhubClient;
    private readonly IAlertNotifier _notifier;
    private readonly ILogger<SyncPricesJob> _logger;
    
    public SyncPricesJobTest(InjectionFixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _wiremockAdminClient = fixture.ServiceProvider.GetRequiredService<WiremockAdminClient>();
        _mockFinnhubClient = fixture.ServiceProvider.GetRequiredService<MockFinnhubClient>();
        var restClient = fixture.ServiceProvider.GetRequiredService<RestClient>();
        _stockClient = new StockClient(restClient);
        _authClient = new AuthClient(restClient);
        var dbContextFactory = new AppDbContextFactory();
        _uow = new UnitOfWork(dbContextFactory.CreateDbContext([]));
        var loggerFactory = new LoggerFactory();
        _logger = loggerFactory.CreateLogger<SyncPricesJob>();
        var notifierLogger = loggerFactory.CreateLogger<NotificationAlertNotifier>();
        _notifier = new NotificationAlertNotifier(notifierLogger);
        _job = new SyncPricesJob(_uow, _mockFinnhubClient, _notifier, _logger);
    }

    [Fact]
    public async Task Should_SyncPrices_When_Called()
    {
        // Arrange
        var loginResponse = await _authClient.LoginAsync(_testUser.Username, _testUser.Password);
        var accessToken = loginResponse.Data!.AccessToken;

        var stockResponse = await _stockClient.GetStocksAsync(accessToken);
        var stocksCount = stockResponse.Data!.TotalItems;

        // Act
        await _job.ExecuteAsync(CancellationToken.None);

        // Assert
        var countResponse = await _wiremockAdminClient.GetCountGetQuotesRequestsAsync();
        countResponse.Data.Count.Should().Be(stocksCount);
    }
}
