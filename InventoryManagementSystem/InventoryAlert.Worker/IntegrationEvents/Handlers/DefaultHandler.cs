using Amazon.SQS.Model;
using InventoryAlert.Worker.Configuration;
using InventoryAlert.Worker.Interfaces;

namespace InventoryAlert.Worker.IntegrationEvents.Handlers;

/// <summary> 
/// Handles unknown/raw SQS messages that don't match known event types. 
/// Standardized for final acknowledgment of messages in the pipeline.
/// </summary> 
public class DefaultHandler(ISqsHelper sqsHelper, ILogger<DefaultHandler> logger, WorkerSettings settings) : IRawDefaultHandler
{
    public async Task HandleAsync(Message message, CancellationToken ct = default)
    {
        logger.LogInformation("[DefaultHandler] Processing raw message {MessageId} from In-Memory Queue. Deleting from AWS...", message.MessageId);

        // Final acknowledgment to remove from SQS
        await sqsHelper.DeleteMessageAsync(settings.Aws.SqsQueueUrl, message.ReceiptHandle, ct);
    }
}
