using FluentAssertions;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Application.Services;
using InventoryAlert.Contracts.Entities;
using InventoryAlert.Contracts.Persistence;
using InventoryAlert.Contracts.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace InventoryAlert.UnitTests.Application.Services;

public class WatchlistServiceTests
{
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IFinnhubClient> _finnhub = new();
    private readonly Mock<IConnectionMultiplexer> _redis = new();
    private readonly Mock<ILogger<WatchlistService>> _logger = new();
    private readonly InventoryDbContext _db;
    private readonly WatchlistService _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public WatchlistServiceTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new InventoryDbContext(options);

        var redisDb = new Mock<IDatabase>();
        _redis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(redisDb.Object);

        _uow.Setup(x => x.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>(async (action, ct) =>
            {
                await action();
                await _db.SaveChangesAsync(ct);
            });

        var mockWatchlistRepo = new Mock<IWatchlistRepository>();
        mockWatchlistRepo.Setup(x => x.GetByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>(async (uid, ct) => (IEnumerable<Watchlist>)await _db.Watchlists.Where(w => w.UserId == uid).Include(w => w.Product).ToListAsync(ct));
        mockWatchlistRepo.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, CancellationToken>((uid, sym, ct) => _db.Watchlists.AnyAsync(w => w.UserId == uid && w.Symbol == sym, ct));
        mockWatchlistRepo.Setup(x => x.AddAsync(It.IsAny<Watchlist>(), It.IsAny<CancellationToken>()))
            .Callback<Watchlist, CancellationToken>((w, _) => _db.Watchlists.Add(w))
            .ReturnsAsync((Watchlist w, CancellationToken _) => w);
        mockWatchlistRepo.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, CancellationToken>((uid, sym, ct) => _db.Watchlists.FirstOrDefaultAsync(w => w.UserId == uid && w.Symbol == sym, ct));
        mockWatchlistRepo.Setup(x => x.DeleteAsync(It.IsAny<Watchlist>()))
            .Callback<Watchlist>(w => _db.Watchlists.Remove(w))
            .ReturnsAsync((Watchlist w) => w);

        _uow.Setup(x => x.Watchlists).Returns(mockWatchlistRepo.Object);

        _sut = new WatchlistService(_uow.Object, _finnhub.Object, _redis.Object, _eventPublisher.Object, _logger.Object);
    }

    [Fact]
    public async Task GetUserWatchlist_ReturnsEmpty_WhenNoItemsExist()
    {
        var result = await _sut.GetUserWatchlistAsync("user-1", Ct);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserWatchlist_ReturnsUserSpecificItems()
    {
        _db.Products.AddRange(
            new Product { TickerSymbol = "AAPL", Name = "Apple" },
            new Product { TickerSymbol = "TSLA", Name = "Tesla" }
        );
        _db.Watchlists.AddRange(
            new Watchlist { UserId = "user-1", Symbol = "AAPL" },
            new Watchlist { UserId = "user-2", Symbol = "TSLA" }
        );
        await _db.SaveChangesAsync(Ct);

        var result = await _sut.GetUserWatchlistAsync("user-1", Ct);

        result.Should().HaveCount(1);
        result[0].Symbol.Should().Be("AAPL");
    }

    [Fact]
    public async Task AddToWatchlist_AddsItem_WhenNotExists()
    {
        _finnhub.Setup(x => x.SearchSymbolsAsync("AAPL", Ct))
            .ReturnsAsync(new FinnhubSymbolSearch { Result = new List<FinnhubSymbolItem> { new FinnhubSymbolItem { Symbol = "AAPL" } } });

        await _sut.AddToWatchlistAsync("user-1", "AAPL", Ct);

        var exists = await _db.Watchlists.AnyAsync(w => w.UserId == "user-1" && w.Symbol == "AAPL");
        exists.Should().BeTrue();

        _eventPublisher.Verify(p => p.PublishAsync(It.Is<InventoryAlert.Contracts.Events.EventEnvelope>(e => e.EventType == InventoryAlert.Contracts.Events.EventTypes.SymbolAdded), Ct), Times.Once);
    }

    [Fact]
    public async Task AddToWatchlist_Skips_WhenAlreadyExists()
    {
        _finnhub.Setup(x => x.SearchSymbolsAsync("AAPL", Ct))
            .ReturnsAsync(new FinnhubSymbolSearch { Result = new List<FinnhubSymbolItem> { new FinnhubSymbolItem { Symbol = "AAPL" } } });

        _db.Watchlists.Add(new Watchlist { UserId = "user-1", Symbol = "AAPL" });
        await _db.SaveChangesAsync(Ct);

        await _sut.AddToWatchlistAsync("user-1", "AAPL", Ct);

        _db.Watchlists.Count().Should().Be(1);
        _eventPublisher.Verify(p => p.PublishAsync(It.IsAny<InventoryAlert.Contracts.Events.EventEnvelope>(), Ct), Times.Never);
    }

    [Fact]
    public async Task RemoveFromWatchlist_RemovesItem_WhenExists()
    {
        _db.Watchlists.Add(new Watchlist { UserId = "user-1", Symbol = "AAPL" });
        await _db.SaveChangesAsync(Ct);

        await _sut.RemoveFromWatchlistAsync("user-1", "AAPL", Ct);

        _db.Watchlists.Count().Should().Be(0);
        _eventPublisher.Verify(p => p.PublishAsync(It.Is<InventoryAlert.Contracts.Events.EventEnvelope>(e => e.EventType == InventoryAlert.Contracts.Events.EventTypes.SymbolRemoved), Ct), Times.Once);
    }
}
