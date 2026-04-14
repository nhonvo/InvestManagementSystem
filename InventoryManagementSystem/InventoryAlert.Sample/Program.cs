using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using InventoryAlert.Domain.Configuration;
using InventoryAlert.Domain.Events;
using InventoryAlert.Domain.Events.Payloads;

// ── Configuration ─────────────────────────────────────────────────────────────
var apiBaseUrl = args.FirstOrDefault() ?? "http://localhost:8080";
var motoUrl = "http://localhost:5000";
var topicArn = "arn:aws:sns:us-east-1:123456789012:inventory-events";

var httpClient = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

var sns = new AmazonSimpleNotificationServiceClient(
    "test", "test",
    new AmazonSimpleNotificationServiceConfig { ServiceURL = motoUrl });

// ── Interactive Menu ──────────────────────────────────────────────────────────
Console.OutputEncoding = System.Text.Encoding.UTF8;

while (true)
{
    Console.WriteLine();
    Console.WriteLine("╔══════════════════════════════════════════╗");
    Console.WriteLine("║   InventoryAlert Event Simulator         ║");
    Console.WriteLine("╠══════════════════════════════════════════╣");
    Console.WriteLine("║  1 → MarketPriceAlert  (via API)         ║");
    Console.WriteLine("║  2 → CompanyNewsAlert  (via API)         ║");
    Console.WriteLine("║  5 → Stress test — 50 random (→ SNS)    ║");
    Console.WriteLine("║  6 → Publish direct to SNS (bypasses API)║");
    Console.WriteLine("║  q → Quit                                ║");
    Console.WriteLine("╚══════════════════════════════════════════╝");
    Console.Write("> ");

    var choice = Console.ReadLine()?.Trim();
    if (choice is "q" or "Q") break;

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    var ct = cts.Token;

    switch (choice)
    {
        case "1":
            await PublishViaApiAsync(httpClient, EventTypes.MarketPriceAlert,
                new MarketPriceAlertPayload
                {
                    Symbol = "AAPL"
                }, ct);
            break;

        case "2":
            await PublishViaApiAsync(httpClient, EventTypes.CompanyNewsAlert,
                new CompanyNewsAlertPayload
                {
                    Symbol = "TSLA"
                }, ct);
            break;

        case "5":
            await StressTestAsync(sns, topicArn, ct);
            break;

        case "6":
            await PublishDirectToSnsAsync(sns, topicArn, ct);
            break;

        default:
            Console.WriteLine("❌ Unknown option.");
            break;
    }
}

// ── Helpers ───────────────────────────────────────────────────────────────────

static async Task PublishViaApiAsync<TPayload>(HttpClient http, string eventType, TPayload payload, CancellationToken ct)
{
    var body = JsonSerializer.Serialize(new
    {
        EventType = eventType,
        Payload = (object)payload!
    }, JsonOptions.Default);

    var response = await http.PostAsync("api/events",
        new StringContent(body, System.Text.Encoding.UTF8, "application/json"), ct);

    Console.WriteLine(response.IsSuccessStatusCode
        ? $"✅ Published {eventType} via API → {(int)response.StatusCode}"
        : $"❌ API error {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(ct)}");
}

static async Task PublishDirectToSnsAsync(IAmazonSimpleNotificationService sns, string topicArn, CancellationToken ct)
{
    var payload = new MarketPriceAlertPayload
    {
        Symbol = "GOOGL"
    };

    var envelope = new EventEnvelope
    {
        EventType = EventTypes.MarketPriceAlert,
        Payload = JsonSerializer.Serialize(payload, JsonOptions.Default),
        CorrelationId = Guid.NewGuid().ToString(),
        Source = "InventoryAlert.Sample"
    };

    await sns.PublishAsync(new PublishRequest
    {
        TopicArn = topicArn,
        Message = JsonSerializer.Serialize(envelope, JsonOptions.Default),
        Subject = envelope.EventType,
        MessageAttributes = new Dictionary<string, MessageAttributeValue>
        {
            ["EventType"] = new() { DataType = "String", StringValue = envelope.EventType }
        }
    }, ct);

    Console.WriteLine($"✅ Published {envelope.EventType} directly to SNS (bypassed API)");
}

static async Task StressTestAsync(IAmazonSimpleNotificationService sns, string topicArn, CancellationToken ct)
{
    var rng = new Random();
    var symbols = new[] { "AAPL", "MSFT", "GOOGL", "TSLA", "AMZN" };
    var types = new[] { EventTypes.MarketPriceAlert, EventTypes.CompanyNewsAlert };

    Console.WriteLine("🚀 Sending 50 random events directly to SNS...");

    for (int i = 0; i < 50; i++)
    {
        var symbol = symbols[rng.Next(symbols.Length)];
        var eventType = types[rng.Next(types.Length)];

        string payloadJson = eventType == EventTypes.MarketPriceAlert
            ? JsonSerializer.Serialize(new MarketPriceAlertPayload
            {
                Symbol = symbol
            }, JsonOptions.Default)
            : JsonSerializer.Serialize(new CompanyNewsAlertPayload
            {
                Symbol = symbol
            }, JsonOptions.Default);

        var envelope = new EventEnvelope
        {
            EventType = eventType,
            Payload = payloadJson,
            CorrelationId = Guid.NewGuid().ToString(),
            Source = "StressTest"
        };

        await sns.PublishAsync(new PublishRequest
        {
            TopicArn = topicArn,
            Message = JsonSerializer.Serialize(envelope, JsonOptions.Default),
            Subject = eventType,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["EventType"] = new() { DataType = "String", StringValue = eventType }
            }
        }, ct);

        Console.WriteLine($"  [{i + 1:D2}/50] {eventType} → {symbol}");
    }

    Console.WriteLine("✅ Stress test complete.");
}
