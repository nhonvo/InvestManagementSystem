using FluentAssertions;
using InventoryAlert.Domain.Common.Constants;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Infrastructure.Utilities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Infrastructure.Utilities;

public class AlertRuleEvaluatorTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IRedisHelper> _redis = new();
    private readonly Mock<ILogger<AlertRuleEvaluator>> _logger = new();
    private readonly AlertRuleEvaluator _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly Guid UserId = Guid.NewGuid();

    public AlertRuleEvaluatorTests()
    {
        _sut = new AlertRuleEvaluator(_uow.Object, _redis.Object, _logger.Object);
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsBreached_WhenPriceAboveTarget()
    {
        // Arrange
        var rule = new AlertRule
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            TickerSymbol = "AAPL",
            Condition = AlertCondition.PriceAbove,
            TargetValue = 150m,
            IsActive = true
        };
        _redis.Setup(r => r.KeyExistsAsync(It.IsAny<string>(), Ct)).ReturnsAsync(false);

        // Act
        var result = await _sut.EvaluateAsync(rule, 160m, Ct);

        // Assert
        result.IsBreached.Should().BeTrue();
        result.Message.Should().Contain("above your target");
        _redis.Verify(r => r.TryAcquireBestEffortLockAsync(It.IsAny<string>(), "1", TimeSpan.FromHours(24), Ct), Times.Once);
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsNotBreached_WhenInCooldown()
    {
        // Arrange
        var rule = new AlertRule
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            TickerSymbol = "AAPL",
            Condition = AlertCondition.PriceAbove,
            TargetValue = 150m,
            IsActive = true
        };
        _redis.Setup(r => r.KeyExistsAsync(It.IsAny<string>(), Ct)).ReturnsAsync(true);

        // Act
        var result = await _sut.EvaluateAsync(rule, 160m, Ct);

        // Assert
        result.IsBreached.Should().BeFalse();
        _redis.Verify(r => r.TryAcquireBestEffortLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), Ct), Times.Never);
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsBreached_WhenPercentDropFromCost()
    {
        // Arrange
        var rule = new AlertRule
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            TickerSymbol = "TSLA",
            Condition = AlertCondition.PercentDropFromCost,
            TargetValue = 10m, // 10% drop
            IsActive = true
        };
        
        var trades = new List<Trade>
        {
            new() { UserId = UserId, TickerSymbol = "TSLA", Type = TradeType.Buy, Quantity = 1, UnitPrice = 100m }
        };

        _uow.Setup(u => u.Trades.GetByUserAndSymbolAsync(UserId, "TSLA", Ct)).ReturnsAsync(trades);
        _redis.Setup(r => r.KeyExistsAsync(It.IsAny<string>(), Ct)).ReturnsAsync(false);

        // Act
        var result = await _sut.EvaluateAsync(rule, 85m, Ct); // 15% drop

        // Assert
        result.IsBreached.Should().BeTrue();
        result.Message.Should().Contain("dropped 15.00%");
    }
}
