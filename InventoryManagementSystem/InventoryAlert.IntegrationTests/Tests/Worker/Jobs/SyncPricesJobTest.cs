using System.Net;
using FluentAssertions;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Events;
using InventoryAlert.IntegrationTests.Abstractions;
using InventoryAlert.IntegrationTests.Fixtures;
using RestSharp;
using Xunit.Abstractions;

namespace InventoryAlert.IntegrationTests.Tests.Worker.Jobs;

public class SyncPricesJobTest : BaseIntegrationTest
{
    public SyncPricesJobTest(InjectionFixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
    }

    [Fact(Skip = "SyncPricesJob is scheduled via Hangfire every 15m. Requires an admin endpoint or test-only trigger for deterministic integration testing.")]
    public async Task SyncPricesJob_ShouldUpdatePrices_AndEvaluateAlerts()
    {
        // Scheduled job testing without deterministic trigger is flaky.
    }
}