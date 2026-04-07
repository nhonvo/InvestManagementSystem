using Amazon.SQS.Model;
using InventoryAlert.Worker.Configuration;
using InventoryAlert.Worker.Interfaces;
using InventoryAlert.Worker.Application.Interfaces.Handlers;

namespace InventoryAlert.Worker.Application.IntegrationHandlers;

/// <summary>
/// Handles unknown/raw SQS messages that don't match known event types.
/// </summary>
public class DefaultHandler(ISqsHelper sqsHelper, ILogger<DefaultHandler> logger, WorkerSettings settings) : IRawDefaultHandler
{
    private readonly ISqsHelper _sqsHelper = sqsHelper;
    private readonly ILogger<DefaultHandler> _logger = logger;
    private readonly WorkerSettings _settings = settings;

    public async Task HandleAsync(Message message, CancellationToken ct = default)
    {
        _logger.LogInformation("[DefaultHandler] Processing raw message {MessageId} from In-Memory Queue.", message.MessageId);

        // Acknowledge the message in SQS as it has reached the final handler
        await _sqsHelper.DeleteMessageAsync(_settings.Aws.SqsQueueUrl, message.ReceiptHandle, ct);
    }
}
