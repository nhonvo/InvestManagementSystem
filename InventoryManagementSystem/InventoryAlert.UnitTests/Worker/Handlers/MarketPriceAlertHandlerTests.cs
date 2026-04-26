using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Events.Payloads;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Worker.IntegrationEvents.Handlers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Worker.Handlers;

public class MarketPriceAlertHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IAlertRuleRepository> _ruleRepoMock = new();
    private readonly Mock<INotificationRepository> _noteRepoMock = new();
    private readonly Mock<IAlertNotifier> _notifierMock = new();
    private readonly Mock<IAlertRuleEvaluator> _evaluatorMock = new();
    private readonly Mock<ILogger<MarketPriceAlertHandler>> _loggerMock = new();
    private readonly MarketPriceAlertHandler _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public MarketPriceAlertHandlerTests()
    {
        // Setup repositories in UoW
        _uowMock.Setup(u => u.AlertRules).Returns(_ruleRepoMock.Object);
        _uowMock.Setup(u => u.Notifications).Returns(_noteRepoMock.Object);

        _sut = new MarketPriceAlertHandler(_uowMock.Object, _notifierMock.Object, _evaluatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenConditionsMet_CreatesNotification_AndUpdatesRule()
    {
        // Arrange
        var symbol = "AAPL";
        var price = 150.00m;
        var userId = Guid.NewGuid();
        var payload = new MarketPriceAlertPayload { Symbol = symbol, NewPrice = price };

        var rule = new AlertRule
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TickerSymbol = symbol,
            Condition = AlertCondition.PriceAbove,
            TargetValue = 140.00m,
            IsActive = true,
            TriggerOnce = true
        };

        _ruleRepoMock.Setup(r => r.GetBySymbolAsync(symbol, Ct))
            .ReturnsAsync(new List<AlertRule> { rule });
        
        _evaluatorMock.Setup(e => e.EvaluateAsync(rule, price, Ct))
            .ReturnsAsync((true, "Price alert: reached..."));

        // Act
        await _sut.HandleAsync(payload, Ct);

        // Assert
        _noteRepoMock.Verify(r => r.AddAsync(It.Is<Notification>(n =>
            n.UserId == userId && n.TickerSymbol == symbol && n.Message.Contains("Price alert")), Ct), Times.Once);
        
        _ruleRepoMock.Verify(r => r.UpdateAsync(It.Is<AlertRule>(rule => 
            rule.TickerSymbol == symbol && rule.IsActive == false && rule.LastTriggeredAt != null), Ct), Times.Once);
        
        _uowMock.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
        _notifierMock.Verify(n => n.NotifyAsync(It.IsAny<Notification>(), Ct), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenConditionNotMet_DoesNothing()
    {
        // Arrange
        var symbol = "AAPL";
        var price = 130.00m; // Below 140
        var payload = new MarketPriceAlertPayload { Symbol = symbol, NewPrice = price };

        var rule = new AlertRule
        {
            TickerSymbol = symbol,
            Condition = AlertCondition.PriceAbove,
            TargetValue = 140.00m,
            IsActive = true
        };

        _ruleRepoMock.Setup(r => r.GetBySymbolAsync(symbol, Ct))
            .ReturnsAsync(new List<AlertRule> { rule });

        _evaluatorMock.Setup(e => e.EvaluateAsync(rule, price, Ct))
            .ReturnsAsync((false, ""));

        // Act
        await _sut.HandleAsync(payload, Ct);

        // Assert
        _noteRepoMock.Verify(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _notifierMock.Verify(n => n.NotifyAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenRuleInactive_DoesNothing()
    {
        // Arrange
        var symbol = "AAPL";
        var price = 150.00m;
        var payload = new MarketPriceAlertPayload { Symbol = symbol, NewPrice = price };

        var rule = new AlertRule
        {
            TickerSymbol = symbol,
            Condition = AlertCondition.PriceAbove,
            TargetValue = 140.00m,
            IsActive = false // INACTIVE
        };

        _ruleRepoMock.Setup(r => r.GetBySymbolAsync(symbol, Ct))
            .ReturnsAsync(new List<AlertRule> { rule });

        // Act
        await _sut.HandleAsync(payload, Ct);

        // Assert
        _noteRepoMock.Verify(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Never);
        _evaluatorMock.Verify(e => e.EvaluateAsync(It.IsAny<AlertRule>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
