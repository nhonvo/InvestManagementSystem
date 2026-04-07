using Amazon.SQS;
using Amazon.SQS.Model;
using InventoryAlert.Worker.Interfaces;

namespace InventoryAlert.Worker.Infrastructure.Messaging;

public class SqsHelper(IAmazonSQS sqs, ILogger<SqsHelper> logger) : ISqsHelper
{
    private readonly List<string> _systemAttributes = ["ApproximateReceiveCount"];
    public async Task<List<Message>> ReceiveMessagesAsync(string queueUrl, int maxMessages = 10, CancellationToken ct = default)
    {
        var request = new ReceiveMessageRequest
        {
            QueueUrl = queueUrl,
            MaxNumberOfMessages = Math.Min(10, maxMessages),
            WaitTimeSeconds = 20, // Enables Long Polling
            MessageSystemAttributeNames = _systemAttributes,
            MessageAttributeNames = ["All"]
        };
        var response = await sqs.ReceiveMessageAsync(request, ct);
        return response.Messages;
    }
    public async Task DeleteMessageAsync(string queueUrl, string receiptHandle, CancellationToken ct = default)
    {
        try
        {
            await sqs.DeleteMessageAsync(queueUrl, receiptHandle, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete message from {QueueUrl}. ReceiptHandle: {ReceiptHandle}", queueUrl, receiptHandle);
            throw; // Rethrow because a failed ACK is a significant infra issue
        }
    }
    public async Task MoveToDlqAsync(Message message, string dlqUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(dlqUrl))
        {
            logger.LogWarning("DLQ URL is not configured. Cannot move message {MessageId}.", message.MessageId);
            return;
        }
        try
        {
            await sqs.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = dlqUrl,
                MessageBody = message.Body,
                MessageAttributes = message.MessageAttributes
            }, ct);

            logger.LogInformation("Successfully moved message {MessageId} to DLQ.", message.MessageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to move message {MessageId} to DLQ {DlqUrl}.", message.MessageId, dlqUrl);
        }
    }
    public int GetReceiveCount(Message message)
    {
        return message.Attributes.TryGetValue("ApproximateReceiveCount", out var countStr) &&
            int.TryParse(countStr, out var count)
            ? count
            : 1;
    }
}
