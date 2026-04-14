using Amazon.SQS;
using Amazon.SQS.Model;
using InventoryAlert.Worker.Interfaces;

namespace InventoryAlert.Worker.Utilities;

public class SqsHelper : ISqsHelper
{
    private readonly IAmazonSQS _sqsClient;
    private readonly ILogger<SqsHelper> _logger;

    public SqsHelper(IAmazonSQS sqsClient, ILogger<SqsHelper> logger)
    {
        _sqsClient = sqsClient;
        _logger = logger;
    }

    public async Task<List<Message>> ReceiveMessagesAsync(string queueUrl, int maxMessages = 10, CancellationToken ct = default)
    {
        try
        {
            var request = new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = maxMessages,
                WaitTimeSeconds = 20,
                AttributeNames = ["ApproximateReceiveCount", "SentTimestamp"]
            };

            var response = await _sqsClient.ReceiveMessageAsync(request, ct);
            return response.Messages ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving messages from SQS: {QueueUrl}", queueUrl);
            return [];
        }
    }

    public async Task DeleteMessageAsync(string queueUrl, string receiptHandle, CancellationToken ct)
    {
        try
        {
            await _sqsClient.DeleteMessageAsync(queueUrl, receiptHandle, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message from SQS: {QueueUrl}", queueUrl);
        }
    }
}
