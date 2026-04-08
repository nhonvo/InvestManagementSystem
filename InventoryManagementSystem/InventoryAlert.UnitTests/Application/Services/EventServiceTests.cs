using FluentAssertions;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Application.Services;
using InventoryAlert.Contracts.Persistence.Interfaces;
using InventoryAlert.Contracts.Events;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Application.Services;

public class EventServiceTests
{
    private readonly Mock<IEventPublisher> _publisher = new();
    private readonly Mock<ICorrelationProvider> _correlation = new();
    private readonly Mock<ILogger<EventService>> _logger = new();
    private readonly EventService _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public EventServiceTests()
    {
        _correlation.Setup(c => c.GetCorrelationId()).Returns("test-correlation-id");
        _sut = new EventService(
            _publisher.Object,
            _correlation.Object,
            _logger.Object);
    }

    // ════════════════════════════════════════════════════════════════
    // PublishEventAsync
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Publish_CallsPublisher_WithCorrectEventType()
    {
        var payload = new { Symbol = "MSFT" };
        EventEnvelope? captured = null;
        _publisher
            .Setup(p => p.PublishAsync(It.IsAny<EventEnvelope>(), Ct))
            .Callback<EventEnvelope, CancellationToken>((env, _) => captured = env)
            .Returns(Task.CompletedTask);

        await _sut.PublishEventAsync("EarningsAlert", payload, Ct);

        captured.Should().NotBeNull();
        captured!.EventType.Should().Be("EarningsAlert");
        captured.CorrelationId.Should().Be("test-correlation-id");
        _publisher.Verify(p => p.PublishAsync(It.IsAny<EventEnvelope>(), Ct), Times.Once);
    }

    [Fact]
    public async Task Publish_SetsCorrectSource_InMessageAttributes()
    {
        EventEnvelope? captured = null;
        _publisher
            .Setup(p => p.PublishAsync(It.IsAny<EventEnvelope>(), Ct))
            .Callback<EventEnvelope, CancellationToken>((env, _) => captured = env)
            .Returns(Task.CompletedTask);

        await _sut.PublishEventAsync("CompanyNewsAlert", new { }, Ct);

        captured!.Source.Should().Be("InventoryAlert.Api");
        captured.EventType.Should().Be("CompanyNewsAlert");
    }

    // ════════════════════════════════════════════════════════════════
    // GetSupportedEventTypesAsync
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetEventTypes_ReturnsAllKnownTypes()
    {
        var result = (await _sut.GetSupportedEventTypesAsync()).ToList();

        result.Should().Contain(EventTypes.MarketPriceAlert);
        result.Should().Contain(EventTypes.StockLowAlert);
        result.Should().Contain(EventTypes.CompanyNewsAlert);
    }
}
