using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Events.Payloads;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;

namespace InventoryAlert.IntegrationTests.Tests.Handlers;

[Collection("IntegrationTests")]
[Trait("Category", "Jobs")]
public class MarketPriceAlertHandlerTests : IAsyncLifetime
{
    private readonly TestFixture _fixture;
    private readonly IServiceProvider _provider;

    public MarketPriceAlertHandlerTests(TestFixture fixture)
    {
        _fixture = fixture;
        _provider = SetupDI.BuildWorkerServiceProvider(_fixture.Configuration, _fixture.LoggerProvider);
    }

    public async Task InitializeAsync() => await _fixture.ResetStateAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    
    public async Task HandleAsync_WhenRuleBreached_CreatesNotificationAndLogs()
    {
        // Arrange
        var unitOfWork = _provider.GetRequiredService<IUnitOfWork>();
        var ct = CancellationToken.None;

        // 1. Seed User
        var user = new User { Id = Guid.NewGuid(), Username = "testuser", PasswordHash = "...", Email = "test@test.com" };
        await unitOfWork.Users.AddAsync(user, ct);

        // 2. Seed StockListing
        var stock = new StockListing { TickerSymbol = "AAPL", Name = "Apple Inc" };
        await unitOfWork.StockListings.AddAsync(stock, ct);

        // 3. Seed Alert Rule (Price Above 100)
        var rule = new AlertRule
        {
            UserId = user.Id,
            TickerSymbol = "AAPL",
            Condition = AlertCondition.PriceAbove,
            TargetValue = 100,
            IsActive = true,
            TriggerOnce = false
        };
        await unitOfWork.AlertRules.AddAsync(rule, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var handler = _provider.GetRequiredService<InventoryAlert.Worker.IntegrationEvents.Handlers.MarketPriceAlertHandler>();
        var payload = new MarketPriceAlertPayload { Symbol = "AAPL", NewPrice = 150m };

        // Act
        await handler.HandleAsync(payload, ct);

        // Assert
        var notesResult = await unitOfWork.Notifications.GetByUserPagedAsync(user.Id.ToString(), true, 1, 10, ct);
        notesResult.Items.Should().ContainSingle(n => n.TickerSymbol == "AAPL" && n.Message.Contains("150"));

        _fixture.LoggerProvider.Entries.Should().Contain(e => e.Message.Contains("Dispatched real-time notification"));
    }

    [Fact]
    
    public async Task HandleAsync_WhenRuleNotBreached_DoesNothing()
    {
        // Arrange
        var unitOfWork = _provider.GetRequiredService<IUnitOfWork>();
        var ct = CancellationToken.None;

        var user = new User { Id = Guid.NewGuid(), Username = "testuser2", PasswordHash = "...", Email = "test2@test.com" };
        await unitOfWork.Users.AddAsync(user, ct);

        var stock = new StockListing { TickerSymbol = "MSFT", Name = "Microsoft" };
        await unitOfWork.StockListings.AddAsync(stock, ct);

        var rule = new AlertRule
        {
            UserId = user.Id,
            TickerSymbol = "MSFT",
            Condition = AlertCondition.PriceAbove,
            TargetValue = 400, // Current price is 150 (in payload)
            IsActive = true
        };
        await unitOfWork.AlertRules.AddAsync(rule, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var handler = _provider.GetRequiredService<InventoryAlert.Worker.IntegrationEvents.Handlers.MarketPriceAlertHandler>();
        var payload = new MarketPriceAlertPayload { Symbol = "MSFT", NewPrice = 150m };

        // Act
        await handler.HandleAsync(payload, ct);

        // Assert
        var notesResult = await unitOfWork.Notifications.GetByUserPagedAsync(user.Id.ToString(), true, 1, 10, ct);
        notesResult.Items.Should().BeEmpty();
    }
}
