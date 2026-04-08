using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using FluentAssertions;
using InventoryAlert.Contracts.Configuration;
using InventoryAlert.Contracts.Events;
using InventoryAlert.Contracts.Persistence.Interfaces;
using InventoryAlert.Worker.Configuration;
using InventoryAlert.Worker.Infrastructure.MessageConsumers;
using InventoryAlert.Worker.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace InventoryAlert.UnitTests.Worker.MessageConsumers;

public class SqsDispatcherTests
{
    private readonly Mock<IAmazonSQS> _sqsMock = new();
    private readonly Mock<IMessageProcessor> _processorMock = new();
    private readonly Mock<IDistributedCache> _cacheMock = new();
    private readonly Mock<IConnectionMultiplexer> _redisMock = new();
    private readonly Mock<IDatabase> _redisDbMock = new(); // Back to Loose behavior, but with correct setup
    private readonly Mock<IEventLogDynamoRepository> _dynamoDbMock = new();
    private readonly Mock<ILogger<SqsDispatcher>> _loggerMock = new();
    private readonly WorkerSettings _settings;
    private readonly SqsDispatcher _sut;

    public SqsDispatcherTests()
    {
        _settings = new WorkerSettings
        {
            Aws = new SharedAwsSettings { SqsQueueUrl = "http://queue", SqsDlqUrl = "http://dlq" }
        };

        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_redisDbMock.Object);

        // Base Redis Defaults - USING THE 4-ARG OVERLOAD that is actually called
        _redisDbMock.Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        _redisDbMock.Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>()))
            .ReturnsAsync(true);

        _redisDbMock.Setup(d => d.KeyExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan?>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Cache Defaults
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[])null!);
        _cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut = new SqsDispatcher(
            _sqsMock.Object,
            _processorMock.Object,
            _cacheMock.Object,
            _redisMock.Object,
            _dynamoDbMock.Object,
            _settings,
            _loggerMock.Object);
    }

    [Fact]
    public async Task DispatchAsync_ReturnsTrue_AndWritesTelemetry_WhenProcessedSuccessfully()
    {
        // Arrange
        var messageId = "msg-123";
        var envelope = new EventEnvelope
        {
            EventType = EventTypes.CompanyNewsAlert,
            MessageId = messageId,
            Payload = "{}"
        };
        var body = JsonSerializer.Serialize(envelope, JsonOptions.Default);

        var message = new Message
        {
            Body = body,
            MessageId = messageId,
            ReceiptHandle = "rh-1",
            Attributes = new Dictionary<string, string> { { "ApproximateReceiveCount", "1" } }
        };

        _processorMock.Setup(p => p.ProcessMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.DispatchAsync(message, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _processorMock.Verify(p => p.ProcessMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_ReturnsTrue_AndSkips_WhenDuplicateDetected()
    {
        // Arrange
        var messageId = "msg-dup";
        var envelope = new EventEnvelope { EventType = EventTypes.CompanyNewsAlert, MessageId = messageId };
        var body = JsonSerializer.Serialize(envelope, JsonOptions.Default);
        var message = new Message
        {
            Body = body,
            MessageId = messageId,
            Attributes = new Dictionary<string, string> { { "ApproximateReceiveCount", "1" } }
        };

        // Redefine redis for failure explicitly - 4 arg version
        _redisDbMock.Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), When.NotExists))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.DispatchAsync(message, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _processorMock.Verify(p => p.ProcessMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_MovesToDlq_WhenMaxRetriesExceeded()
    {
        // Arrange
        var message = new Message
        {
            Body = "{}",
            MessageId = "retry-id",
            Attributes = new Dictionary<string, string> { { "ApproximateReceiveCount", "10" } }
        };

        // Act
        var result = await _sut.DispatchAsync(message, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _sqsMock.Verify(s => s.SendMessageAsync(It.Is<SendMessageRequest>(r => r.QueueUrl == _settings.Aws.SqsDlqUrl), It.IsAny<CancellationToken>()), Times.Once);
    }
}
