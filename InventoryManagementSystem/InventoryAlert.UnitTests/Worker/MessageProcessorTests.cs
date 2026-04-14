using System.Text.Json;
using Amazon.SQS.Model;
using FluentAssertions;
using Hangfire;
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
    private readonly Mock<IBackgroundTaskQueue> _taskQueueMock = new();
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
    public async Task ProcessAndAcknowledgeAsync_ReturnsTrue_WhenMessageTypeMissing()
    {
        // Arrange
        var message = new Message { MessageId = "1", Body = "{}" };

        // Act
        var result = await _sut.ProcessAndAcknowledgeAsync(message, Ct);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessAndAcknowledgeAsync_ReturnsTrue_AndLogsError_WhenPayloadIsInvalidJson()
    {
        // Arrange
        var message = new Message
        {
            MessageId = "1",
            Body = "invalid-json",
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                { "MessageType", new MessageAttributeValue { StringValue = EventTypes.MarketPriceAlert } }
            }
        };

        // Act
        var result = await _sut.ProcessAndAcknowledgeAsync(message, Ct);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessAndAcknowledgeAsync_ReturnsTrue_ForStockLowAlert()
    {
        // Arrange
        var payload = new LowHoldingsAlertPayload(Guid.NewGuid(), "AAPL", 10, 5);
        var message = new Message
        {
            MessageId = "1",
            Body = JsonSerializer.Serialize(payload, InventoryAlert.Domain.Configuration.JsonOptions.Default),
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                { "MessageType", new MessageAttributeValue { StringValue = EventTypes.StockLowAlert } }
            }
        };

        // Act
        var result = await _sut.ProcessAndAcknowledgeAsync(message, Ct);

        // Assert
        result.Should().BeTrue();
    }
}
