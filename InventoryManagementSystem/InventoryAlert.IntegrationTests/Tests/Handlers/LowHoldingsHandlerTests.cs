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
public class LowHoldingsHandlerTests : IAsyncLifetime
{
    private readonly TestFixture _fixture;
    private readonly IServiceProvider _provider;

    public LowHoldingsHandlerTests(TestFixture fixture)
    {
        _fixture = fixture;
        _provider = SetupDI.BuildWorkerServiceProvider(_fixture.Configuration, _fixture.LoggerProvider);
    }

    public async Task InitializeAsync() => await _fixture.ResetStateAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    
    public async Task HandleAsync_CreatesNotification_AndDispatchesToNotifier()
    {
        // Arrange
        var unitOfWork = _provider.GetRequiredService<IUnitOfWork>();
        var ct = CancellationToken.None;

        var userId = Guid.NewGuid();

        // Seed User to satisfy FK constraint
        await unitOfWork.Users.AddAsync(new User { Id = userId, Username = "lowholdingsuser", Email = "lh@test.com", PasswordHash = "..." }, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var payload = new LowHoldingsAlertPayload(userId, "MSFT", 10, 5);

        var handler = _provider.GetRequiredService<InventoryAlert.Worker.IntegrationEvents.Handlers.LowHoldingsHandler>();

        // Act
        await handler.HandleAsync(payload, ct);

        // Assert
        var notesResult = await unitOfWork.Notifications.GetByUserPagedAsync(userId.ToString(), true, 1, 10, ct);
        notesResult.Items.Should().ContainSingle(n => 
            n.TickerSymbol == "MSFT" && 
            n.Message.Contains("balance has reached 5") &&
            n.Message.Contains("threshold of 10"));

        _fixture.LoggerProvider.Entries.Should().Contain(e => e.Message.Contains("[LowHoldingsHandler] Processing alert for MSFT"));
    }
}
