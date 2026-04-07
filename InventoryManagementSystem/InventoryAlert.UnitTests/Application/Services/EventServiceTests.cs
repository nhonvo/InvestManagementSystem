using FluentAssertions;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Application.Services;
using InventoryAlert.Api.Domain.Interfaces;
using InventoryAlert.Contracts.Events;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Application.Services;

public class EventServiceTests
{
    private readonly Mock<IEventPublisher> _publisher = new();
    private readonly Mock<IEventLogRepository> _eventLogRepository = new();
    private readonly Mock<ILogger<EventService>> _logger = new();
    private readonly EventService _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public EventServiceTests()
    {
        _sut = new EventService(
            _publisher.Object,
            _eventLogRepository.Object,
            new Mock<IEventLogQuery>().Object,
            _logger.Object);

        // Default: AddAsync returns the same EventLog
        _eventLogRepository
            .Setup(r => r.AddAsync(It.IsAny<EventLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventLog log, CancellationToken _) => log);
    }

    // ════════════════════════════════════════════════════════════════
    // PublishEventAsync
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Publish_PersistsEventLog()
    {
        var payload = new { Symbol = "AAPL", DropPercent = 0.15m };

        await _sut.PublishEventAsync("MarketPriceAlert", payload, Ct);

        _eventLogRepository.Verify(r =>
            r.AddAsync(It.IsAny<EventLog>(), Ct), Times.Once);
    }

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

    [Fact]
    public async Task Publish_PersistsLog_WithCorrectFields()
    {
        EventLog? captured = null;
        _eventLogRepository
            .Setup(r => r.AddAsync(It.IsAny<EventLog>(), Ct))
            .Callback<EventLog, CancellationToken>((log, _) => captured = log)
            .ReturnsAsync(new EventLog());

        await _sut.PublishEventAsync("InsiderSellAlert", new { Symbol = "GOOGL" }, Ct);

        captured.Should().NotBeNull();
        captured!.EventType.Should().Be("InsiderSellAlert");
        captured.Status.Should().Be("Published");
        captured.Source.Should().Be("InventoryAlert.Api");
        captured.MessageId.Should().NotBeNullOrWhiteSpace();
        captured.Payload.Should().Contain("GOOGL");
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
