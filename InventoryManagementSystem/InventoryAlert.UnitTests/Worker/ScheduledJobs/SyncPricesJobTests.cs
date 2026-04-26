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

    public SyncPricesJobTests()
    {
        _uow.Setup(u => u.StockListings).Returns(new Mock<IStockListingRepository>().Object);
        _uow.Setup(u => u.PriceHistories).Returns(new Mock<IPriceHistoryRepository>().Object);
        _uow.Setup(u => u.AlertRules).Returns(new Mock<IAlertRuleRepository>().Object);
        _uow.Setup(u => u.Notifications).Returns(new Mock<INotificationRepository>().Object);
        _uow.Setup(u => u.Trades).Returns(new Mock<ITradeRepository>().Object);

        _service = new SyncPricesJob(_uow.Object, _finnhub.Object, _notifier.Object, _logger.Object);
    }

    [Fact]
    public async Task Execute_RecordsPriceHistory_ForActiveListings()
    {
        // Arrange
        var listing = new StockListing { TickerSymbol = "AAPL" };
        _uow.Setup(u => u.StockListings.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<StockListing> { listing });
        _finnhub.Setup(f => f.GetQuoteAsync("AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryAlert.Domain.External.Finnhub.FinnhubQuoteResponse { CurrentPrice = 150m });
        
        _uow.Setup(u => u.AlertRules.GetBySymbolsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AlertRule>());

        // Act
        var result = await _service.ExecuteAsync(CancellationToken.None);

        // Assert
        result.Status.Should().Be(InventoryAlert.Worker.Models.JobStatus.Success);
        _uow.Verify(u => u.PriceHistories.AddRangeAsync(It.Is<IEnumerable<PriceHistory>>(p => p.Any(x => x.TickerSymbol == "AAPL" && x.Price == 150m)), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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

        _uow.Setup(u => u.StockListings.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<StockListing> { listing });
        _finnhub.Setup(f => f.GetQuoteAsync("TSLA", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryAlert.Domain.External.Finnhub.FinnhubQuoteResponse { CurrentPrice = 210m });
        
        _uow.Setup(u => u.AlertRules.GetBySymbolsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AlertRule> { rule });

        // Act
        await _service.ExecuteAsync(CancellationToken.None);

        // Assert
        _uow.Verify(u => u.Notifications.AddRangeAsync(It.Is<IEnumerable<Notification>>(n => n.Any(x => x.TickerSymbol == "TSLA" && x.AlertRuleId == rule.Id)), It.IsAny<CancellationToken>()), Times.Once);
        _notifier.Verify(n => n.NotifyAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
