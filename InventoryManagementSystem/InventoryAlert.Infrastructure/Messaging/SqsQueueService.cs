using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using InventoryAlert.Domain.Configuration;
using InventoryAlert.Domain.Events;
using InventoryAlert.Domain.Events.Payloads;
using InventoryAlert.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace InventoryAlert.Infrastructure.Messaging;

public class SqsQueueService(
    IAmazonSQS sqs,
    AppSettings settings,
    ICorrelationProvider correlationProvider,
    ILogger<SqsQueueService> logger) : IQueueService
{
    private readonly IAmazonSQS _sqs = sqs;
    private readonly AppSettings _settings = settings;
    private readonly ICorrelationProvider _correlationProvider = correlationProvider;
    private readonly ILogger<SqsQueueService> _logger = logger;

    public async Task EnqueueAlertEvaluationAsync(string symbol, decimal price, CancellationToken ct = default)
    {
        var payload = new MarketPriceAlertPayload
        {
            Symbol = symbol,
            NewPrice = price
        };

        var envelope = new EventEnvelope
        {
            EventType = EventTypes.MarketPriceAlert,
            Payload = JsonSerializer.Serialize(payload),
            CorrelationId = _correlationProvider.GetCorrelationId(),
            Source = "InventoryAlert.Worker"
        };

        var body = JsonSerializer.Serialize(envelope);
        await SendMessageAsync(string.Empty, body, ct);
    }

    public async Task SendMessageAsync(string queueName, string messageBody, CancellationToken ct = default)
    {
        try
        {
            var queueUrl = !string.IsNullOrEmpty(queueName) && queueName != "inventory-events"
                ? await GetQueueUrlAsync(queueName, ct)
                : _settings.Aws.SqsQueueUrl;

            // Simple validation to prevent empty URL failures
            if (string.IsNullOrEmpty(queueUrl))
            {
                _logger.LogWarning("[SqsQueueService] Skipping message: SQS Queue URL is empty.");
                return;
            }

            var request = new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = messageBody
            };

            await _sqs.SendMessageAsync(request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SqsQueueService] Failed to send message to queue.");
            // Do not throw to prevent critical worker failure on infra blips
        }
    }

    private async Task<string> GetQueueUrlAsync(string queueName, CancellationToken ct)
    {
        try
        {
            var response = await _sqs.GetQueueUrlAsync(queueName, ct);
            return response.QueueUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SqsQueueService] Could not resolve URL for queue: {QueueName}", queueName);
            return string.Empty;
        }
    }
}
