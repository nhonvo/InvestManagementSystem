using Amazon.SQS;
using Amazon.SQS.Model;
using FluentAssertions;
using InventoryAlert.Contracts.Configuration;
using InventoryAlert.Worker.Configuration;
using InventoryAlert.Worker.Infrastructure.MessageConsumers;
using InventoryAlert.Worker.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Worker.MessageConsumers;

public class PollSqsJobTests
{
    private readonly Mock<ISqsHelper> _sqsMock = new();
    private readonly Mock<ISqsDispatcher> _dispatcherMock = new();
    private readonly Mock<ILogger<PollSqsJob>> _loggerMock = new();
    private readonly WorkerSettings _settings;
    private readonly PollSqsJob _sut;

    public PollSqsJobTests()
    {
        _settings = new WorkerSettings
        {
            Aws = new SharedAwsSettings { SqsQueueUrl = "http://queue" }
        };

        _sut = new PollSqsJob(
            _sqsMock.Object,
            _dispatcherMock.Object,
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
        _dispatcherMock.Verify(d => d.ProcessBatchAsync(messages, It.IsAny<CancellationToken>()), Times.Once);
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
        _dispatcherMock.Verify(d => d.ProcessBatchAsync(It.IsAny<IEnumerable<Message>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
