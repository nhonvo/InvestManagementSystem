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

namespace InventoryAlert.IntegrationTests.Tests.Jobs;

[Trait("Category", "Jobs")]
public class WholeFlowTests : Tier2TestBase
{
    public WholeFlowTests(TestFixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
    }

    private async Task<(string Username, string Token)> SeedUserAndLoginAsync()
    {
        var uow = Fixture.ServiceProvider.GetRequiredService<IUnitOfWork>();
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
        var uow = Fixture.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var ct = CancellationToken.None;

        var (username, token) = await SeedUserAndLoginAsync();
        var user = await uow.Users.GetByUsernameAsync(username, ct);

        await uow.StockListings.AddAsync(new StockListing { TickerSymbol = "FLOW", Name = "Flow" }, ct);
        await uow.AlertRules.AddAsync(new AlertRule { UserId = user!.Id, TickerSymbol = "FLOW", Condition = AlertCondition.PriceAbove, TargetValue = 100, IsActive = true }, ct);
        await uow.SaveChangesAsync(ct);

        await Client.ExecutePostAsync(new RestRequest("events").AddHeader("Authorization", $"Bearer {token}").AddJsonBody(new
        {
            EventType = "inventoryalert.pricing.price-drop.v1",
            Payload = new { Symbol = "FLOW", NewPrice = 150m }
        }));

        bool found = false;
        for (int i = 0; i < 20; i++)
        {
            var noteRes = await Client.ExecuteGetAsync(new RestRequest("notifications").AddHeader("Authorization", $"Bearer {token}"));
            if (noteRes.IsSuccessStatusCode && !string.IsNullOrEmpty(noteRes.Content))
            {
                var data = JsonConvert.DeserializeObject<PagedResult<NotificationResponse>>(noteRes.Content);
                if (data != null && data.Items.Any(n => n.TickerSymbol == "FLOW"))
                {
                    found = true;
                    break;
                }
            }
            await Task.Delay(5000);
        }

        found.Should().BeTrue();
    }

    [Fact]
    
    public async Task PriceSync_FullFlow_Succeeds()
    {
        var uow = Fixture.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var ct = CancellationToken.None;

        var (username, token) = await SeedUserAndLoginAsync();
        var user = await uow.Users.GetByUsernameAsync(username, ct);

        await uow.StockListings.AddAsync(new StockListing { TickerSymbol = "AAPL", Name = "Apple" }, ct);
        await uow.AlertRules.AddAsync(new AlertRule 
        { 
            UserId = user!.Id, 
            TickerSymbol = "AAPL", 
            Condition = AlertCondition.PriceAbove, 
            TargetValue = 180, 
            IsActive = true 
        }, ct);
        await uow.SaveChangesAsync(ct);

        var wireMockAdminUrl = Fixture.Configuration["WiremockSettings:BaseUrl"] ?? "http://localhost:9091";
        var adminClient = new RestClient(wireMockAdminUrl);
        
        var mapping = new
        {
            request = new { method = "GET", urlPath = "/quote", queryParameters = new { symbol = new { equalTo = "AAPL" } } },
            response = new { status = 200, body = "{\"c\": 195.5, \"h\": 198, \"l\": 190, \"o\": 192, \"pc\": 191}", headers = new { Content_Type = "application/json" } }
        };
        await adminClient.ExecutePostAsync(new RestRequest("__admin/mappings").AddJsonBody(mapping));

        var syncRes = await Client.ExecutePostAsync(new RestRequest("stocks/sync").AddHeader("Authorization", $"Bearer {token}"));
        syncRes.StatusCode.Should().Be(HttpStatusCode.Accepted);

        bool found = false;
        for (int i = 0; i < 20; i++)
        {
            var noteRes = await Client.ExecuteGetAsync(new RestRequest("notifications").AddHeader("Authorization", $"Bearer {token}"));
            if (noteRes.IsSuccessStatusCode && !string.IsNullOrEmpty(noteRes.Content))
            {
                var data = JsonConvert.DeserializeObject<PagedResult<NotificationResponse>>(noteRes.Content);
                if (data != null && data.Items.Any(n => n.TickerSymbol == "AAPL" && n.Message.Contains("195.5")))
                {
                    found = true;
                    break;
                }
            }
            await Task.Delay(5000);
        }

        found.Should().BeTrue();

        var history = await uow.PriceHistories.GetBySymbolAsync("AAPL", 1, ct);
        history.Should().ContainSingle(h => h.Price == 195.5m);

        var logFound = await Fixture.LogReader.WaitForLogFragmentAsync("Starting price synchronization");
        logFound.Should().BeTrue("Worker should log the start of sync in Seq.");
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
        var uow = Fixture.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var ct = CancellationToken.None;

        var (username, token) = await SeedUserAndLoginAsync();
        var symbol = "CRASH";

        await uow.StockListings.AddAsync(new StockListing { TickerSymbol = symbol, Name = "Fail Test" }, ct);
        await uow.SaveChangesAsync(ct);

        var wireMockAdminUrl = Fixture.Configuration["WiremockSettings:BaseUrl"] ?? "http://localhost:9091";
        var adminClient = new RestClient(wireMockAdminUrl);
        
        var mapping = new
        {
            request = new { method = "GET", urlPath = "/quote", queryParameters = new { symbol = new { equalTo = symbol } } },
            response = new { status = 500, body = "Internal Server Error" }
        };
        await adminClient.ExecutePostAsync(new RestRequest("__admin/mappings").AddJsonBody(mapping));

        await Client.ExecutePostAsync(new RestRequest("stocks/sync").AddHeader("Authorization", $"Bearer {token}"));

        var logFound = await Fixture.LogReader.WaitForLogFragmentAsync($"Failed to fetch quote for {symbol}", timeoutSeconds: 60);
        logFound.Should().BeTrue($"Worker should log the Finnhub failure for {symbol} in Seq.");

        var history = await uow.PriceHistories.GetBySymbolAsync(symbol, 10, ct);
        history.Should().BeEmpty();
    }
}
