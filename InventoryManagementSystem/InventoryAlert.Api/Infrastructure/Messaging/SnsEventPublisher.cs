using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Web.Configuration;
using InventoryAlert.Contracts.Events;

namespace InventoryAlert.Api.Infrastructure.Messaging;

/// <summary>
/// Publishes an EventEnvelope to the SNS topic (Moto in dev, real SNS in prod).
/// The SNS topic fans-out automatically to the subscribed SQS queue consumed by the Worker.
/// </summary>
public sealed class SnsEventPublisher(
    IAmazonSimpleNotificationService sns,
    AppSettings settings,
    ILogger<SnsEventPublisher> logger) : IEventPublisher
{
    private readonly IAmazonSimpleNotificationService _sns = sns;
    private readonly string _topicArn = settings.Aws.SnsTopicArn;
    private readonly ILogger<SnsEventPublisher> _logger = logger;

    public async Task PublishAsync(EventEnvelope envelope, CancellationToken ct = default)
    {
        var messageBody = JsonSerializer.Serialize(envelope);

        var request = new PublishRequest
        {
            TopicArn = _topicArn,
            Message = messageBody,
            Subject = envelope.EventType,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["EventType"] = new()
                {
                    DataType = "String",
                    StringValue = envelope.EventType
                },
                ["Source"] = new()
                {
                    DataType = "String",
                    StringValue = envelope.Source
                },
                ["CorrelationId"] = new()
                {
                    DataType = "String",
                    StringValue = envelope.CorrelationId
                }
            }
        };

        var response = await _sns.PublishAsync(request, ct);
        _logger.LogInformation(
            "[SnsEventPublisher] Published {EventType} | CorrelationId={CorrelationId} | SnsMessageId={SnsId}",
            envelope.EventType, envelope.CorrelationId, response.MessageId);
    }
}
