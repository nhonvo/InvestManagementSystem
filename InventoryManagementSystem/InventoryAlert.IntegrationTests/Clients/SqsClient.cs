using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.Runtime;

namespace InventoryAlert.IntegrationTests.Clients;

public class SqsClient : IDisposable
{
    private readonly AmazonSQSClient _sqs;
    private const string QueueUrl = "http://localhost:5000/123456789012/event-queue";
    private const string DlqUrl = "http://localhost:5000/123456789012/inventory-event-dlq";

    public SqsClient()
    {
        var credentials = new BasicAWSCredentials("test", "test");
        var config = new AmazonSQSConfig
        {
            ServiceURL = "http://localhost:5000",
            AuthenticationRegion = "us-east-1"
        };
        _sqs = new AmazonSQSClient(credentials, config);
    }

    public async Task PurgeMainQueueAsync()
    {
        await _sqs.PurgeQueueAsync(QueueUrl);
    }

    public async Task PurgeDlqAsync()
    {
        await _sqs.PurgeQueueAsync(DlqUrl);
    }

    public async Task<List<Message>> ReceiveMessagesFromMainAsync(int maxMessages = 1, int waitTimeSeconds = 1)
    {
        var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = QueueUrl,
            MaxNumberOfMessages = maxMessages,
            WaitTimeSeconds = waitTimeSeconds,
            AttributeNames = new List<string> { "All" },
            MessageAttributeNames = new List<string> { "All" }
        });
        return response.Messages;
    }

    public async Task<List<Message>> ReceiveMessagesFromDlqAsync(int maxMessages = 1, int waitTimeSeconds = 1)
    {
        var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = DlqUrl,
            MaxNumberOfMessages = maxMessages,
            WaitTimeSeconds = waitTimeSeconds,
            AttributeNames = new List<string> { "All" },
            MessageAttributeNames = new List<string> { "All" }
        });
        return response.Messages;
    }

    public void Dispose()
    {
        _sqs.Dispose();
        GC.SuppressFinalize(this);
    }
}
