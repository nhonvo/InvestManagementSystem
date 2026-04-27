using System.Net;
using FluentAssertions;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Events;
using InventoryAlert.IntegrationTests.Abstractions;
using InventoryAlert.IntegrationTests.Clients;
using InventoryAlert.IntegrationTests.Fixtures;
using RestSharp;
using Xunit.Abstractions;

namespace InventoryAlert.IntegrationTests.Tests.Worker.Events;

public class EventDlqTest : BaseIntegrationTest, IDisposable
{
    private readonly SqsClient _sqsClient = new();

    public EventDlqTest(InjectionFixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
    }

    [Fact(Skip = "Already covered by E2E test SqsRetryE2ETests.PoisonMessage_ShouldRetry_And_EndUpInDLQ, but keeping structure as planned")]
    public void PoisonMessage_ShouldGoToDlq()
    {
        // E2E covers this comprehensively
    }

    public void Dispose()
    {
        _sqsClient.Dispose();
        GC.SuppressFinalize(this);
    }
}