using FluentAssertions;
using InventoryAlert.Api.Services;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Application.Services;

public class AlertRuleServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IQueueService> _queueService = new();
    private readonly Mock<IFinnhubClient> _finnhubClient = new();
    private readonly Mock<ILogger<AlertRuleService>> _logger = new();
    private readonly AlertRuleService _sut;
    private static readonly string TestUserId = Guid.NewGuid().ToString();
    private static readonly CancellationToken Ct = CancellationToken.None;

    public AlertRuleServiceTests()
    {
        _sut = new AlertRuleService(_unitOfWork.Object);
        _unitOfWork
            .Setup(u => u.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((action, _) => action());
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenListingMissing()
    {
        var request = new AlertRuleRequest("NEW", AlertCondition.PriceAbove, 100m, true);
        _unitOfWork.Setup(u => u.StockListings.FindBySymbolAsync("NEW", Ct))
            .ReturnsAsync((StockListing?)null);

        Func<Task> act = () => _sut.CreateAsync(request, TestUserId, Ct);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Symbol NEW must be resolved*");
    }

    [Fact]
    public async Task CreateAlert_AddsRule_WhenListingExists()
    {
        var request = new AlertRuleRequest("AAPL", AlertCondition.PriceAbove, 150m, false);
        var listing = new StockListing { TickerSymbol = "AAPL" };

        _unitOfWork.Setup(u => u.StockListings.FindBySymbolAsync("AAPL", Ct))
            .ReturnsAsync(listing);

        _unitOfWork.Setup(u => u.AlertRules.AddAsync(It.IsAny<AlertRule>(), Ct))
            .ReturnsAsync(new AlertRule { Id = Guid.NewGuid() });

        await _sut.CreateAsync(request, TestUserId, Ct);

        _unitOfWork.Verify(u => u.AlertRules.AddAsync(It.IsAny<AlertRule>(), Ct), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesAllFields_WhenRuleExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existing = new AlertRule { Id = id, UserId = Guid.Parse(TestUserId), TickerSymbol = "OLD", TargetValue = 100m };
        var request = new AlertRuleRequest("NEW", AlertCondition.PriceBelow, 200m, false);

        _unitOfWork.Setup(u => u.AlertRules.GetByIdAsync(id, Ct)).ReturnsAsync(existing);

        // Act
        var res = await _sut.UpdateAsync(id, request, TestUserId, Ct);

        // Assert
        res.TickerSymbol.Should().Be("NEW");
        res.Condition.Should().Be(AlertCondition.PriceBelow);
        res.TargetValue.Should().Be(200m);
        _unitOfWork.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }
}

