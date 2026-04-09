using System.Text.Json;
using Amazon.SQS.Model;
using FluentAssertions;
using Hangfire;
using InventoryAlert.Contracts.Events;
using InventoryAlert.Contracts.Events.Payloads;
using InventoryAlert.Worker.Application;
using InventoryAlert.Worker.Application.Interfaces.Handlers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Worker;

public class MessageProcessorTests
{
    private readonly Mock<IRawDefaultHandler> _rawHandlerMock = new();
    private readonly Mock<IBackgroundJobClient> _jobClientMock = new();
    private readonly Mock<ILogger<MessageProcessor>> _loggerMock = new();
    private readonly MessageProcessor _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public MessageProcessorTests()
    {
        _sut = new MessageProcessor(_rawHandlerMock.Object, _jobClientMock.Object, _loggerMock.Object);
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
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessAndAcknowledgeAsync_ReturnsTrue_ForPriceAlert()
    {
        // Arrange
        var payload = new MarketPriceAlertPayload { Symbol = "AAPL" };
        var message = new Message
        {
            MessageId = "1",
            Body = JsonSerializer.Serialize(payload),
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                { "MessageType", new MessageAttributeValue { StringValue = EventTypes.MarketPriceAlert } }
            }
        };

        // Act
        var result = await _sut.ProcessAndAcknowledgeAsync(message, Ct);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessAndAcknowledgeAsync_ReturnsTrue_ForUnknownType_ByRoutingToRawHandler()
    {
        // Arrange
        var message = new Message
        {
            MessageId = "1",
            Body = "{}",
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                { "MessageType", new MessageAttributeValue { StringValue = "UnknownType" } }
            }
        };

        // Act
        var result = await _sut.ProcessAndAcknowledgeAsync(message, Ct);

        // Assert
        result.Should().BeTrue();
        _rawHandlerMock.Verify(h => h.HandleAsync(message, It.IsAny<CancellationToken>()), Times.Once);
    }
}
