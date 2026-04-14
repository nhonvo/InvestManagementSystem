using FluentAssertions;
using InventoryAlert.Api.Controllers;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Interfaces;
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

    [Fact]
    public async Task PublishEvent_Returns202_WhenValidRequest()
    {
        var request = new PublishEventRequest { EventType = "price-alert", Payload = new { Symbol = "AAPL" } };
        _service.Setup(s => s.PublishEventAsync(It.IsAny<string>(), It.IsAny<object>(), Ct))
            .Returns(Task.CompletedTask);

        var result = await _sut.PublishEvent(request, Ct);

        result.Should().BeOfType<AcceptedResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status202Accepted);
    }

    [Fact]
    public async Task GetEventTypes_Returns200_WithEventTypes()
    {
        var types = (IEnumerable<string>)["price-alert", "news-alert"];
        _service.Setup(s => s.GetSupportedEventTypesAsync()).ReturnsAsync(types);

        var result = await _sut.GetEventTypes(Ct);

        var ok = (result.Result as OkObjectResult);
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(StatusCodes.Status200OK);
        (ok.Value as IEnumerable<string>).Should().BeEquivalentTo(types);
    }
}


