using System.Net;
using Amazon.SQS;
using Amazon.SQS.Model;
using FluentAssertions;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Events;
using InventoryAlert.E2ETests.Abstractions;
using RestSharp;
using Xunit;

namespace InventoryAlert.E2ETests;

public class SqsRetryE2ETests : BaseE2ETest
{
    private readonly IAmazonSQS _sqs;
    private const string QueueUrl = "http://localhost:5000/123456789012/event-queue";
    private const string DlqUrl = "http://localhost:5000/123456789012/inventory-event-dlq";

    public SqsRetryE2ETests()
    {
        _sqs = new AmazonSQSClient(
            new Amazon.Runtime.BasicAWSCredentials("test", "test"),
            new AmazonSQSConfig
            {
                ServiceURL = "http://localhost:5000"
            });
    }

    [Fact]
    public async Task PoisonMessage_ShouldRetry_And_EndUpInDLQ()
    {
        // 1. Arrange
        await EnsureAuthenticatedAsync();

        // Ensure queues are clean before starting
        await _sqs.PurgeQueueAsync(QueueUrl);
        await _sqs.PurgeQueueAsync(DlqUrl);
        
        // Wait a bit for purge to settle in Moto
        await Task.Delay(2000);

        // 2. Act - Publish a message that we know will fail in the Worker
        var publishRequest = CreateAuthenticatedRequest("api/v1/events", Method.Post);
        publishRequest.AddJsonBody(new PublishEventRequest
        {
            EventType = EventTypes.TestFailureRequested,
            Payload = new { Reason = "Trigger DLQ Test" }
        });

        var publishResponse = await Client.ExecuteAsync(publishRequest);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // 3. Assert - Poll DLQ for the message
        // SQS Redrive Policy is set to maxReceiveCount: 3
        // So we wait for it to transition.
        bool foundInDlq = false;
        var retryCount = 0;
        
        while (retryCount < 50) // 50 attempts * 3s = 150s max wait
        {
            var receiveResponse = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = DlqUrl,
                MaxNumberOfMessages = 1,
                WaitTimeSeconds = 1
            });

            if (receiveResponse?.Messages != null && receiveResponse.Messages.Any())
            {
                var message = receiveResponse.Messages.First();
                if (message.Body.Contains(EventTypes.TestFailureRequested))
                {
                    foundInDlq = true;
                    break;
                }
            }

            retryCount++;
            await Task.Delay(3000);
        }

        foundInDlq.Should().BeTrue("Poison message should have been moved to the Dead Letter Queue after 3 retries.");
    }
}
