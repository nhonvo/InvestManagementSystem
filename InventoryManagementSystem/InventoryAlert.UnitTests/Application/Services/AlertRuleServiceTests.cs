using FluentAssertions;
using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Application.Services;
using InventoryAlert.Contracts.Entities;
using InventoryAlert.Contracts.Events;
using InventoryAlert.Contracts.Persistence;
using InventoryAlert.Contracts.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Application.Services;

public class AlertRuleServiceTests
{
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<AlertRuleService>> _logger = new();
    private readonly InventoryDbContext _db;
    private readonly AlertRuleService _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public AlertRuleServiceTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new InventoryDbContext(options);

        _uow.Setup(x => x.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>(async (action, ct) =>
            {
                await action();
                await _db.SaveChangesAsync(ct);
            });

        var mockAlertRepo = new Mock<IAlertRuleRepository>();
        mockAlertRepo.Setup(x => x.GetByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>(async (uid, ct) => (IEnumerable<AlertRule>)await _db.AlertRules.Where(a => a.UserId == uid).ToListAsync(ct));
        mockAlertRepo.Setup(x => x.AddAsync(It.IsAny<AlertRule>(), It.IsAny<CancellationToken>()))
            .Callback<AlertRule, CancellationToken>((r, _) => _db.AlertRules.Add(r))
            .ReturnsAsync((AlertRule r, CancellationToken _) => r);
        mockAlertRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns<Guid, CancellationToken>((id, ct) => _db.AlertRules.FirstOrDefaultAsync(a => a.Id == id, ct));
        mockAlertRepo.Setup(x => x.UpdateAsync(It.IsAny<AlertRule>()))
            .Callback<AlertRule>(r => _db.AlertRules.Update(r))
            .ReturnsAsync((AlertRule r) => r);
        mockAlertRepo.Setup(x => x.DeleteAsync(It.IsAny<AlertRule>()))
            .Callback<AlertRule>(r => _db.AlertRules.Remove(r))
            .ReturnsAsync((AlertRule r) => r);

        var mockWatchlistRepo = new Mock<IWatchlistRepository>();
        mockWatchlistRepo.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, CancellationToken>((uid, sym, ct) => _db.Watchlists.AnyAsync(w => w.UserId == uid && w.Symbol == sym, ct));

        _uow.Setup(x => x.AlertRules).Returns(mockAlertRepo.Object);
        _uow.Setup(x => x.Watchlists).Returns(mockWatchlistRepo.Object);

        _sut = new AlertRuleService(_uow.Object, _eventPublisher.Object, _logger.Object);
    }

    [Fact]
    public async Task GetUserAlerts_ReturnsEmpty_WhenNoRulesExist()
    {
        var result = await _sut.GetUserAlertsAsync("user-1", Ct);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAlert_AddsRule_AndPublishesEvent()
    {
        var request = new AlertRuleRequest("AAPL", "Price", "Below", 150m, "email");
        _db.Watchlists.Add(new Watchlist { UserId = "user-1", Symbol = "AAPL" });
        await _db.SaveChangesAsync(Ct);

        var result = await _sut.CreateAlertAsync("user-1", request, Ct);

        result.Symbol.Should().Be("AAPL");
        _db.AlertRules.Count().Should().Be(1);
        _eventPublisher.Verify(p => p.PublishAsync(It.Is<EventEnvelope>(e => e.EventType == EventTypes.AlertRuleCreated), Ct), Times.Once);
    }

    [Fact]
    public async Task UpdateAlert_UpdatesRule_AndPublishesEvent()
    {
        var ruleId = Guid.NewGuid();
        _db.AlertRules.Add(new AlertRule { Id = ruleId, UserId = "user-1", Symbol = "AAPL", Field = "Price", Operator = "Below", Threshold = 150m });
        await _db.SaveChangesAsync(Ct);

        var request = new AlertRuleRequest("AAPL", "Price", "Above", 200m, "sms");
        var result = await _sut.UpdateAlertAsync("user-1", ruleId, request, Ct);

        result.Threshold.Should().Be(200m);
        result.Operator.Should().Be("Above");
        _eventPublisher.Verify(p => p.PublishAsync(It.Is<EventEnvelope>(e => e.EventType == EventTypes.AlertRuleUpdated), Ct), Times.Once);
    }

    [Fact]
    public async Task DeleteAlert_RemovesRule_AndPublishesEvent()
    {
        var ruleId = Guid.NewGuid();
        _db.AlertRules.Add(new AlertRule { Id = ruleId, UserId = "user-1", Symbol = "AAPL" });
        await _db.SaveChangesAsync(Ct);

        await _sut.DeleteAlertAsync("user-1", ruleId, Ct);

        _db.AlertRules.Any(a => a.Id == ruleId).Should().BeFalse();
        _eventPublisher.Verify(p => p.PublishAsync(It.Is<EventEnvelope>(e => e.EventType == EventTypes.AlertRuleDeleted), Ct), Times.Once);
    }
}
