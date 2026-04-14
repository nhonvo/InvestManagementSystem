using Amazon.SQS.Model;
using InventoryAlert.Domain.Configuration;
using InventoryAlert.Worker.Configuration;
using InventoryAlert.Worker.Interfaces;
using InventoryAlert.Worker.ScheduledJobs;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Worker.MessageConsumers;

public class SqsScheduledPollerTests
{
    private readonly Mock<ISqsHelper> _sqsMock = new();
    private readonly Mock<IProcessQueueJob> _processQueueJobMock = new();
    private readonly Mock<ILogger<SqsScheduledPollerJob>> _loggerMock = new();
    private readonly WorkerSettings _settings;
    private readonly SqsScheduledPollerJob _sut;

    public SqsScheduledPollerTests()
    {
        _settings = new WorkerSettings
        {
            Aws = new SharedAwsSettings { SqsQueueUrl = "http://queue" }
        };

        _sut = new SqsScheduledPollerJob(
            _sqsMock.Object,
            _processQueueJobMock.Object,
            _settings,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_CallsDispatcher_WhenMessagesReceived()
    {
        // Arrange
        var messages = new List<Message> { new Message { Body = "{}", ReceiptHandle = "rh-1" } };
        _sqsMock.Setup(s => s.ReceiveMessagesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        // Act
        await _sut.ExecuteAsync(CancellationToken.None);

        // Assert
        _processQueueJobMock.Verify(d => d.ProcessBatchAsync(messages, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Skips_WhenNoMessages()
    {
        // Arrange
        _sqsMock.Setup(s => s.ReceiveMessagesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Message>());

        // Act
        await _sut.ExecuteAsync(CancellationToken.None);

        // Assert
        _processQueueJobMock.Verify(d => d.ProcessBatchAsync(It.IsAny<IEnumerable<Message>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}




