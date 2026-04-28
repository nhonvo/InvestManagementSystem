using System.Text.Json;
using Amazon.SQS.Model;
using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using InventoryAlert.Domain.Events;
using InventoryAlert.Domain.Events.Payloads;
using InventoryAlert.Worker.IntegrationEvents.Routing;
using InventoryAlert.Worker.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Worker;

public class IntegrationMessageRouterTests
{
    private readonly Mock<IRawDefaultHandler> _rawHandlerMock = new();
    private readonly Mock<IBackgroundJobClient> _jobClientMock = new();
    private readonly Mock<ILogger<IntegrationMessageRouter>> _loggerMock = new();
    private readonly IntegrationMessageRouter _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public IntegrationMessageRouterTests()
    {
        _sut = new IntegrationMessageRouter(
            _rawHandlerMock.Object,
            _jobClientMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessAndAcknowledgeAsync_ReturnsTrue_WhenEnvelopeInvalid()
    {
        // Arrange
        var message = new Message { MessageId = "1", Body = "{ \"eventType\": \"\" }" };

        // Act
        var result = await _sut.ProcessAndAcknowledgeAsync(message, Ct);

        // Assert
        result.Should().BeTrue("Router should ACK (return true) invalid envelopes to prevent queue blockage");
        _rawHandlerMock.Verify(h => h.HandleAsync(It.IsAny<Message>(), Ct), Times.Once);
    }

    [Fact]
    public async Task RouteEnvelopeAsync_ReturnsTrue_ForStockLowAlert()
    {
        // Arrange
        var payload = new LowHoldingsAlertPayload(Guid.NewGuid(), "AAPL", 10, 5);
        var envelope = new EventEnvelope
        {
            EventType = EventTypes.StockLowAlert,
            Payload = JsonSerializer.Serialize(payload, InventoryAlert.Domain.Configuration.JsonOptions.Default)
        };

        // Act
        var result = await _sut.RouteEnvelopeAsync(envelope, Ct);

        // Assert
        result.Should().BeTrue();
        _jobClientMock.Verify(c => c.Create(
            It.Is<Job>(j => j.Type == typeof(InventoryAlert.Worker.IntegrationEvents.Handlers.LowHoldingsHandler)),
            It.IsAny<IState>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAndAcknowledgeAsync_UnwrapsSns_AndRoutes()
    {
        // Arrange
        var envelope = new EventEnvelope { EventType = EventTypes.SyncMarketNewsRequested, Payload = "{}" };
        var snsWrapper = new
        {
            Type = "Notification",
            Message = JsonSerializer.Serialize(envelope, InventoryAlert.Domain.Configuration.JsonOptions.Default)
        };
        var message = new Message { Body = JsonSerializer.Serialize(snsWrapper) };

        // Act
        var result = await _sut.ProcessAndAcknowledgeAsync(message, Ct);

        // Assert
        result.Should().BeTrue();
        _jobClientMock.Verify(c => c.Create(
            It.Is<Job>(j => j.Type == typeof(InventoryAlert.Worker.ScheduledJobs.NewsSyncJob)),
            It.IsAny<IState>()), Times.Once);
    }
}
