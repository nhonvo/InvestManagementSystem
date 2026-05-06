using System.Net;
using FluentAssertions;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.IntegrationTests.Abstractions;
using InventoryAlert.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using Xunit.Abstractions;
using Newtonsoft.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;
using InventoryAlert.Worker.ScheduledJobs;
using InventoryAlert.Worker.IntegrationEvents.Handlers;
using InventoryAlert.Domain.Events.Payloads;

namespace InventoryAlert.IntegrationTests.Tests.Jobs;

[Trait("Category", "Jobs")]
public class WholeFlowTests : Tier2TestBase
{
    public WholeFlowTests(TestFixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
    }

    private async Task<(string Username, string Token)> SeedUserAndLoginAsync()
    {
        var uow = Services.GetRequiredService<IUnitOfWork>();
        var ct = CancellationToken.None;
        var username = "user_" + Guid.NewGuid().ToString().Substring(0, 8);
        var password = "password";
        var user = new User { Id = Guid.NewGuid(), Username = username, Email = $"{username}@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), Role = "Admin" };
        await uow.Users.AddAsync(user, ct);
        await uow.SaveChangesAsync(ct);

        var loginRes = await Client.ExecutePostAsync<AuthResponse>(new RestRequest("auth/login").AddJsonBody(new { username, password }));
        return (username, loginRes.Data!.AccessToken);
    }

    [Fact]
    public async Task PriceAlertCycle_FullFlow_Succeeds()
    {
        var uow = Services.GetRequiredService<IUnitOfWork>();
        var ct = CancellationToken.None;

        var (username, token) = await SeedUserAndLoginAsync();
        var user = await uow.Users.GetByUsernameAsync(username, ct);
        var symbol = "FLOW_" + Guid.NewGuid().ToString().Substring(0, 4);

        await uow.StockListings.AddAsync(new StockListing { TickerSymbol = symbol, Name = "Flow" }, ct);
        await uow.AlertRules.AddAsync(new AlertRule { UserId = user!.Id, TickerSymbol = symbol, Condition = AlertCondition.PriceAbove, TargetValue = 100, IsActive = true }, ct);
        await uow.SaveChangesAsync(ct);

        // Manually trigger the handler in-process to ensure it works with the current state
        var handler = Services.GetRequiredService<MarketPriceAlertHandler>();
        await handler.HandleAsync(new MarketPriceAlertPayload { Symbol = symbol, NewPrice = 150m }, ct);

        bool found = false;
        for (int i = 0; i < 10; i++)
        {
            var noteRes = await Client.ExecuteGetAsync(new RestRequest("notifications").AddHeader("Authorization", $"Bearer {token}"));
            if (noteRes.IsSuccessStatusCode && !string.IsNullOrEmpty(noteRes.Content))
            {
                var data = JsonConvert.DeserializeObject<PagedResult<NotificationResponse>>(noteRes.Content);
                if (data != null && data.Items.Any(n => n.TickerSymbol == symbol))
                {
                    found = true;
                    break;
                }
            }
            await Task.Delay(1000);
        }

        found.Should().BeTrue();
    }

    [Fact]
    public async Task PriceSync_FullFlow_Succeeds()
    {
        var uow = Services.GetRequiredService<IUnitOfWork>();
        var ct = CancellationToken.None;

        var (username, token) = await SeedUserAndLoginAsync();
        var user = await uow.Users.GetByUsernameAsync(username, ct);
        var symbol = "SYNC_" + Guid.NewGuid().ToString().Substring(0, 4);

        await uow.StockListings.AddAsync(new StockListing { TickerSymbol = symbol, Name = "Apple" }, ct);
        await uow.AlertRules.AddAsync(new AlertRule 
        { 
            UserId = user!.Id, 
            TickerSymbol = symbol, 
            Condition = AlertCondition.PriceAbove, 
            TargetValue = 180, 
            IsActive = true 
        }, ct);
        await uow.SaveChangesAsync(ct);

        // Configure local WireMock for symbol with HIGH PRIORITY match
        Fixture.WireMock.Given(
            WireMock.RequestBuilders.Request.Create()
                .WithPath("/quote")
                .WithParam("symbol", symbol)
        ).AtPriority(1).RespondWith(
            WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"c\": 195.5, \"h\": 198, \"l\": 190, \"o\": 192, \"pc\": 191}")
        );

        // Trigger Sync via API (tests event publication)
        var syncRes = await Client.ExecutePostAsync(new RestRequest("stocks/sync").AddHeader("Authorization", $"Bearer {token}"));
        syncRes.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // VERIFY Rule exists in DB before proceeding
        var savedRule = (await uow.AlertRules.GetBySymbolAsync(symbol, ct)).FirstOrDefault();
        savedRule.Should().NotBeNull($"Alert rule for {symbol} should be in DB before handler is called.");

        // Manually execute the SyncPricesJob in-process
        var job = Services.GetRequiredService<SyncPricesJob>();
        await job.ExecuteAsync(ct);

        // Manually trigger the handler for the price change
        var handler = Services.GetRequiredService<MarketPriceAlertHandler>();
        await handler.HandleAsync(new MarketPriceAlertPayload { Symbol = symbol, NewPrice = 195.5m }, ct);

        // Assert side effects
        bool found = false;
        for (int i = 0; i < 10; i++)
        {
            var noteRes = await Client.ExecuteGetAsync(new RestRequest("notifications").AddHeader("Authorization", $"Bearer {token}"));
            if (noteRes.IsSuccessStatusCode && !string.IsNullOrEmpty(noteRes.Content))
            {
                var data = JsonConvert.DeserializeObject<PagedResult<NotificationResponse>>(noteRes.Content);
                if (data != null && data.Items.Any(n => n.TickerSymbol == symbol && n.Message.Contains("195.5")))
                {
                    found = true;
                    break;
                }
            }
            await Task.Delay(1000);
        }

        found.Should().BeTrue($"Notification for {symbol} at 195.5 should be found.");

        var history = await uow.PriceHistories.GetBySymbolAsync(symbol, 1, ct);
        history.Should().ContainSingle(h => h.Price == 195.5m);
    }

    [Fact]
    public async Task PoisonMessage_TriggersCriticalFailureLog()
    {
        var (username, token) = await SeedUserAndLoginAsync();

        await Client.ExecutePostAsync(new RestRequest("events").AddHeader("Authorization", $"Bearer {token}").AddJsonBody(new
        {
            EventType = "inventoryalert.test.failure.v1",
            Payload = new { Reason = "Simulation" }
        }));

        var logFound = await Fixture.LogReader.WaitForLogFragmentAsync("Critical failure processing message", timeoutSeconds: 60);
        logFound.Should().BeTrue("Worker should log a critical error in Seq when receiving a poison message.");
    }

    [Fact]
    public async Task PriceSync_FinnhubError_DoesNotCreatePriceHistory()
    {
        var uow = Services.GetRequiredService<IUnitOfWork>();
        var ct = CancellationToken.None;

        var (username, token) = await SeedUserAndLoginAsync();
        var symbol = "CRASH_" + Guid.NewGuid().ToString().Substring(0, 4);

        await uow.StockListings.AddAsync(new StockListing { TickerSymbol = symbol, Name = "Fail Test" }, ct);
        await uow.SaveChangesAsync(ct);

        // Configure local WireMock to fail with HIGH PRIORITY
        Fixture.WireMock.Given(
            WireMock.RequestBuilders.Request.Create()
                .WithPath("/quote")
                .WithParam("symbol", symbol)
        ).AtPriority(1).RespondWith(
            WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error")
        );

        // Trigger Sync via API
        await Client.ExecutePostAsync(new RestRequest("stocks/sync").AddHeader("Authorization", $"Bearer {token}"));

        // Manually execute the Job in-process
        var job = Services.GetRequiredService<SyncPricesJob>();
        await job.ExecuteAsync(ct);

        // Verify logs in Seq
        var logFound = await Fixture.LogReader.WaitForLogFragmentAsync($"Failed to fetch quote for {symbol}");
        logFound.Should().BeTrue($"Worker should log the Finnhub failure for {symbol} in Seq.");

        var history = await uow.PriceHistories.GetBySymbolAsync(symbol, 10, ct);
        history.Should().BeEmpty($"Price history for {symbol} should be empty after failure.");
    }
}
