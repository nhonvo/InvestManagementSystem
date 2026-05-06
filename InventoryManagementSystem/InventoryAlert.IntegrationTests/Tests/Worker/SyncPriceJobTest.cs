using FluentAssertions;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Infrastructure.Persistence.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;
using InventoryAlert.Infrastructure.Utilities;
using InventoryAlert.IntegrationTests.Abstractions;
using InventoryAlert.IntegrationTests.Clients;
using InventoryAlert.IntegrationTests.Config;
using InventoryAlert.IntegrationTests.Fixtures;
using InventoryAlert.Worker.Configuration;
using InventoryAlert.Worker.Models;
using InventoryAlert.Worker.ScheduledJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace InventoryAlert.IntegrationTests.Tests.Worker;

public class SyncPriceJobTest : BaseIntegrationTest
{
    private readonly AppDbContext _db;
    private readonly SyncPricesJob _job;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFinnhubClient _finnhubClient;
    private readonly AppSettings _settings;

    private readonly Mock<IAlertNotifier> _notifier = new();
    private readonly IAlertRuleEvaluator _evaluator;
    private readonly Mock<ILogger<SyncPricesJob>> _logger = new();

    private readonly Mock<IRedisHelper> _redis = new();
    private readonly Mock<ILogger<AlertRuleEvaluator>> _alertRuleLogger = new();

    private readonly WiremockAdminClient _wiremockAdminClient;

    public SyncPriceJobTest(InjectionFixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        //var unitOfWork = new UnitOfWork
        _settings = fixture.ServiceProvider.GetRequiredService<AppSettings>();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_settings.Database.DefaultConnection)
            .Options;
        _db = new AppDbContext(options);
        CleanDatabase().GetAwaiter().GetResult(); // Clean DB

        _unitOfWork = new UnitOfWork(_db);

        _finnhubClient = fixture.ServiceProvider.GetRequiredService<MockFinnhubClient>();

        _evaluator = new AlertRuleEvaluator(_unitOfWork, _redis.Object, _alertRuleLogger.Object);

        var workerSettings = new WorkerSettings
        {
            MaxDegreeOfParallelism = 1,
        };

        _job = new SyncPricesJob(_unitOfWork, _finnhubClient, _notifier.Object, _evaluator, workerSettings, _logger.Object);

        _wiremockAdminClient = fixture.ServiceProvider.GetRequiredService<WiremockAdminClient>();
    }

    [Fact]
    public async Task ExecuteAsync_Should_CallMockAPI_And_UpdatePriceData()
    {
        // Arrange
        var ct = CancellationToken.None;
        var tickerSymbol = "TSLA";

        // Seed Data
        var stock = new StockListing
        {
            TickerSymbol = tickerSymbol,
            Name = "Tesla Inc",
            Exchange = "NASDAQ NMS - GLOBAL MARKET",
            Currency = "USD",
            Country = "US",
            Industry = "Automobiles"
        };
        await _unitOfWork.StockListings.AddAsync(stock, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        await _wiremockAdminClient.ResetAsync();

        // Act
        var result = await _job.ExecuteAsync(ct);

        // Assert
        result.Status.Should().Be(JobStatus.Success);

        var countResponse = await _wiremockAdminClient.GetCountOfGetQuotesRequestsAsync();
        countResponse.Data!.Count.Should().Be(1);

        var priceHistories = await _unitOfWork.PriceHistories.GetAllAsync(ct);
        priceHistories.Should().HaveCount(1);
        priceHistories.Should().Contain(x => x.TickerSymbol == tickerSymbol && x.Price == 259.2M);
    }

    [Fact]
    public async Task ExecuteAsync_Should_SendNotification_If_AlertRuleTrigger()
    {
        // Arrange
        var ct = CancellationToken.None;
        var tickerSymbol = "TSLA";

        // Seed Data
        var stock = new StockListing
        {
            TickerSymbol = tickerSymbol,
            Name = "Tesla Inc",
            Exchange = "NASDAQ NMS - GLOBAL MARKET",
            Currency = "USD",
            Country = "US",
            Industry = "Automobiles"
        };
        await _unitOfWork.StockListings.AddAsync(stock, ct);

        var users = await _unitOfWork.Users.GetAllAsync(ct);
        var userId = users.First().Id;
        var alertRule = new AlertRule
        {
            UserId = userId,
            TickerSymbol = tickerSymbol,
            Condition = AlertCondition.PriceAbove,
            TargetValue = 200
        };
        await _unitOfWork.AlertRules.AddAsync(alertRule, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        await _wiremockAdminClient.ResetAsync();

        // Act
        var result = await _job.ExecuteAsync(ct);

        // Assert
        result.Status.Should().Be(JobStatus.Success);

        var countResponse = await _wiremockAdminClient.GetCountOfGetQuotesRequestsAsync();
        countResponse.Data!.Count.Should().Be(1);

        var priceHistories = await _unitOfWork.PriceHistories.GetAllAsync(ct);
        priceHistories.Should().HaveCount(1);
        priceHistories.Should().Contain(x => x.TickerSymbol == tickerSymbol && x.Price == 259.2M);

        var notifications = await _unitOfWork.Notifications.GetAllAsync(ct);
        notifications.Should().HaveCount(1);
        notifications.Should().Contain(x => x.TickerSymbol == tickerSymbol);

        var updatedRule = (await _unitOfWork.AlertRules.GetAllAsync(ct)).First();
        updatedRule.IsActive.Should().BeFalse();
        updatedRule.LastTriggeredAt.Should().NotBeNull();
    }

    private async Task CleanDatabase()
    {
        _db.Notifications.RemoveRange(_db.Notifications);
        _db.PriceHistories.RemoveRange(_db.PriceHistories);
        _db.AlertRules.RemoveRange(_db.AlertRules);
        _db.StockListings.RemoveRange(_db.StockListings);

        await _db.SaveChangesAsync();
    }
}
