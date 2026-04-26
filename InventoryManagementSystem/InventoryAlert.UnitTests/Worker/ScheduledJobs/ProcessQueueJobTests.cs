using System.Text.Json;
using Amazon.SQS.Model;
using InventoryAlert.Domain.Configuration;
using InventoryAlert.Domain.Events;
using InventoryAlert.Worker.Configuration;
using InventoryAlert.Worker.Interfaces;
using InventoryAlert.Worker.ScheduledJobs;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;
using FluentAssertions;

namespace InventoryAlert.UnitTests.Worker.ScheduledJobs;

public class ProcessQueueJobTests
{
    private readonly Mock<ISqsHelper> _sqsHelperMock = new();
    private readonly Mock<IIntegrationMessageRouter> _routerMock = new();
    private readonly Mock<IConnectionMultiplexer> _redisMock = new();
    private readonly Mock<IDatabase> _redisDbMock = new();
    private readonly Mock<ILogger<ProcessQueueJob>> _loggerMock = new();
    private readonly WorkerSettings _settings;
    private readonly ProcessQueueJob _sut;

    public ProcessQueueJobTests()
    {
        _settings = new WorkerSettings
        {
            Aws = new SharedAwsSettings { SqsQueueUrl = "http://queue" }
        };

        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_redisDbMock.Object);
        
        _sut = new ProcessQueueJob(
            _sqsHelperMock.Object,
            _routerMock.Object,
            _redisMock.Object,
            _settings,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessBatchAsync_AcknowledgesAndDeletes_WhenProcessedSuccessfully()
    {
        // Arrange
        var messageId = "msg-123";

        var envelope = new EventEnvelope
        {
            EventType = EventTypes.MarketPriceAlert,
            MessageId = messageId,
            Payload = "{ \"Symbol\": \"AAPL\" }"
        };
        var body = JsonSerializer.Serialize(envelope, JsonOptions.Default);
        var message = new Message
        {
            Body = body,
            MessageId = messageId,
            ReceiptHandle = "rh-1",
            Attributes = new Dictionary<string, string> { { "ApproximateReceiveCount", "1" } }
        };

        _routerMock.Setup(r => r.RouteEnvelopeAsync(It.IsAny<EventEnvelope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _redisDbMock.Setup(d => d.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        // Act
        await _sut.ProcessBatchAsync(new[] { message }, CancellationToken.None);

        // Assert
        _sqsHelperMock.Verify(s => s.DeleteMessageAsync(_settings.Aws.SqsQueueUrl, "rh-1", It.IsAny<CancellationToken>()), Times.Once);
        
        // We bypass strict signature matching by checking if ANY call happened to the mock that looks like a save
        _redisDbMock.Invocations.Any(i => i.Method.Name.Contains("StringSet") && i.Arguments[0].ToString().Contains(messageId))
            .Should().BeTrue("Redis should have marked the message as processed.");
    }

    [Fact]
    public async Task ProcessBatchAsync_Skips_WhenDuplicateDetected()
    {
        // Arrange
        var messageId = "msg-dup";
        var envelope = new EventEnvelope { EventType = EventTypes.MarketPriceAlert, MessageId = messageId, Payload = "{ \"Symbol\": \"AAPL\" }" };
        var body = JsonSerializer.Serialize(envelope, JsonOptions.Default);
        var message = new Message
        {
            Body = body,
            MessageId = messageId,
            ReceiptHandle = "rh-dup",
            Attributes = new Dictionary<string, string> { { "ApproximateReceiveCount", "1" } }
        };

        _redisDbMock.Setup(d => d.KeyExistsAsync(It.Is<RedisKey>(k => k.ToString().Contains(messageId)), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true); // Exists = Duplicate

        // Act
        await _sut.ProcessBatchAsync(new[] { message }, CancellationToken.None);

        // Assert
        _routerMock.Verify(r => r.RouteEnvelopeAsync(It.IsAny<EventEnvelope>(), It.IsAny<CancellationToken>()), Times.Never);
        _sqsHelperMock.Verify(s => s.DeleteMessageAsync(It.IsAny<string>(), "rh-dup", It.IsAny<CancellationToken>()), Times.Once);
    }
}
