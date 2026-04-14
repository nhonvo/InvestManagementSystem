using FluentAssertions;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Worker.ScheduledJobs;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Worker.ScheduledJobs;

public class SyncPricesJobTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IFinnhubClient> _finnhub = new();
    private readonly Mock<IAlertNotifier> _notifier = new();
    private readonly Mock<ILogger<SyncPricesJob>> _logger = new();
    private readonly SyncPricesJob _service;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public SyncPricesJobTests()
    {
        _uow.Setup(u => u.StockListings).Returns(new Mock<IStockListingRepository>().Object);
        _uow.Setup(u => u.PriceHistories).Returns(new Mock<IPriceHistoryRepository>().Object);
        _uow.Setup(u => u.AlertRules).Returns(new Mock<IAlertRuleRepository>().Object);
        _uow.Setup(u => u.Notifications).Returns(new Mock<INotificationRepository>().Object);

        _service = new SyncPricesJob(_uow.Object, _finnhub.Object, _notifier.Object, _logger.Object);
    }

    [Fact]
    public async Task Execute_RecordsPriceHistory_ForActiveListings()
    {
        // Arrange
        var listing = new StockListing { TickerSymbol = "AAPL" };
        _uow.Setup(u => u.StockListings.GetAllAsync(Ct)).ReturnsAsync(new List<StockListing> { listing });
        _finnhub.Setup(f => f.GetQuoteAsync("AAPL", Ct))
            .ReturnsAsync(new InventoryAlert.Domain.External.Finnhub.FinnhubQuoteResponse { CurrentPrice = 150m });
        _uow.Setup(u => u.AlertRules.GetBySymbolAsync("AAPL", Ct)).ReturnsAsync(new List<AlertRule>());

        // Act
        var result = await _service.ExecuteAsync(Ct);

        // Assert
        result.Status.Should().Be(InventoryAlert.Worker.Models.JobStatus.Success);
        _uow.Verify(u => u.PriceHistories.AddAsync(It.Is<PriceHistory>(p => p.TickerSymbol == "AAPL" && p.Price == 150m), Ct), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }

    [Fact]
    public async Task Execute_TriggersAlert_WhenThresholdReached()
    {
        // Arrange
        var listing = new StockListing { TickerSymbol = "TSLA" };
        var rule = new AlertRule
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TickerSymbol = "TSLA",
            Condition = AlertCondition.PriceAbove,
            TargetValue = 200m,
            IsActive = true
        };

        _uow.Setup(u => u.StockListings.GetAllAsync(Ct)).ReturnsAsync(new List<StockListing> { listing });
        _finnhub.Setup(f => f.GetQuoteAsync("TSLA", Ct))
            .ReturnsAsync(new InventoryAlert.Domain.External.Finnhub.FinnhubQuoteResponse { CurrentPrice = 210m });
        _uow.Setup(u => u.AlertRules.GetBySymbolAsync("TSLA", Ct)).ReturnsAsync(new List<AlertRule> { rule });

        // Act
        await _service.ExecuteAsync(Ct);

        // Assert
        _uow.Verify(u => u.Notifications.AddAsync(It.Is<Notification>(n => n.TickerSymbol == "TSLA" && n.AlertRuleId == rule.Id), Ct), Times.Once);
        _notifier.Verify(n => n.NotifyAsync(It.IsAny<Notification>(), Ct), Times.Once);
    }
}
