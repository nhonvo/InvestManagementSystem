using FluentAssertions;
using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Web.Controllers;
using InventoryAlert.Contracts.Events.Payloads;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Web.Controllers;

public class EventsControllerTests
{
    private readonly Mock<IEventService> _service = new();
    private readonly EventsController _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public EventsControllerTests()
    {
        _sut = new EventsController(_service.Object);
    }

    // ══════════════════════════════════════════════════════════════
    // POST api/events
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task PublishEvent_Returns202_WhenValidRequest()
    {
        var request = new PublishEventRequest { EventType = InventoryAlert.Contracts.Events.EventTypes.MarketPriceAlert, Payload = new { } };
        _service.Setup(s => s.PublishEventAsync(It.IsAny<string>(), It.IsAny<object>(), Ct))
            .Returns(Task.CompletedTask);

        var result = await _sut.PublishEvent(request, Ct);

        result.Should().BeOfType<AcceptedResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status202Accepted);
    }

    [Fact]
    public async Task PublishEvent_Returns400_WhenEventTypeEmpty()
    {
        var request = new PublishEventRequest { EventType = string.Empty, Payload = new { } };

        var result = await _sut.PublishEvent(request, Ct);

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        _service.Verify(s => s.PublishEventAsync(
            It.IsAny<string>(), It.IsAny<object>(), Ct), Times.Never);
    }

    // ══════════════════════════════════════════════════════════════
    // POST api/events/market-alert
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task TriggerMarketAlert_Returns202_Always()
    {
        var request = new MarketAlertRequest
        {
            ProductId = 1,
            Symbol = "AAPL"
        };

        var result = await _sut.TriggerMarketAlert(request, Ct);

        result.Should().BeOfType<AcceptedResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status202Accepted);
    }

    [Fact]
    public async Task TriggerMarketAlert_PublishesMarketPriceAlert_WithMappedPayload()
    {
        var request = new MarketAlertRequest
        {
            ProductId = 7,
            Symbol = "TSLA"
        };


        await _sut.TriggerMarketAlert(request, Ct);

        _service.Verify(s => s.TriggerMarketAlertAsync(
            It.Is<MarketAlertRequest>(r => r.ProductId == 7 && r.Symbol == "TSLA"), Ct), Times.Once);

    }

    // ══════════════════════════════════════════════════════════════
    // POST api/events/news-alert
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task TriggerNewsAlert_Returns202_Always()
    {
        var request = new NewsAlertRequest
        {
            Symbol = "AAPL"
        };

        var result = await _sut.TriggerNewsAlert(request, Ct);

        result.Should().BeOfType<AcceptedResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status202Accepted);
    }

    [Fact]
    public async Task TriggerNewsAlert_PublishesCompanyNewsAlert_WithMappedPayload()
    {
        var request = new NewsAlertRequest
        {
            Symbol = "GOOGL"
        };


        await _sut.TriggerNewsAlert(request, Ct);

        _service.Verify(s => s.TriggerNewsAlertAsync(
            It.Is<NewsAlertRequest>(r => r.Symbol == "GOOGL"), Ct), Times.Once);

    }

    // ══════════════════════════════════════════════════════════════
    // GET api/events/types
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetEventTypes_Returns200_WithEventTypes()
    {
        var types = (IEnumerable<string>)[InventoryAlert.Contracts.Events.EventTypes.MarketPriceAlert, InventoryAlert.Contracts.Events.EventTypes.CompanyNewsAlert];
        _service.Setup(s => s.GetSupportedEventTypesAsync()).ReturnsAsync(types);

        var result = await _sut.GetEventTypes(Ct);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.As<IEnumerable<string>>().Should().BeEquivalentTo(types);
    }
}
