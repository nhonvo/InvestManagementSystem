using System.Net;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using InventoryAlert.Domain.Configuration;
using InventoryAlert.Domain.Events;
using InventoryAlert.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Infrastructure.Messaging;

public class SnsEventPublisherTests
{
    private readonly Mock<IAmazonSimpleNotificationService> _snsMock = new();
    private readonly AppSettings _settings = new() { Aws = new() { SnsTopicArn = "arn:aws:sns:topic" } };
    private readonly Mock<ILogger<SnsEventPublisher>> _loggerMock = new();
    private readonly SnsEventPublisher _sut;

    public SnsEventPublisherTests()
    {
        _sut = new SnsEventPublisher(_snsMock.Object, _settings, _loggerMock.Object);
    }

    [Fact]
    public async Task PublishAsync_SendsCorrectRequest()
    {
        // Arrange
        var envelope = new EventEnvelope
        {
            EventType = "TestEvent",
            Source = "TestSource",
            CorrelationId = Guid.NewGuid().ToString(),
            Payload = "{\"data\":\"test\"}"
        };

        _snsMock.Setup(x => x.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublishResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                MessageId = "msg-id"
            });

        // Act
        await _sut.PublishAsync(envelope);

        // Assert
        _snsMock.Verify(x => x.PublishAsync(It.Is<PublishRequest>(r =>
            r.TopicArn == "arn:aws:sns:topic" &&
            r.Subject == envelope.EventType &&
            r.MessageAttributes["EventType"].StringValue == envelope.EventType &&
            r.MessageAttributes["Source"].StringValue == envelope.Source &&
            r.MessageAttributes["CorrelationId"].StringValue == envelope.CorrelationId
        ), It.IsAny<CancellationToken>()), Times.Once);
    }
}


