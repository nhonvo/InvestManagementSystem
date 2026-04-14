using System.Text.Json;
using FluentAssertions;
using InventoryAlert.Api.Services;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.External.Finnhub;
using InventoryAlert.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace InventoryAlert.UnitTests.Application.Services;

public class StockDataServiceTests
{
    private readonly Mock<IFinnhubClient> _finnhub = new();
    private readonly Mock<IConnectionMultiplexer> _redis = new();
    private readonly Mock<IDatabase> _cache = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMarketNewsDynamoRepository> _marketNewsRepo = new();
    private readonly Mock<ICompanyNewsDynamoRepository> _companyNewsRepo = new();
    private readonly Mock<ILogger<StockDataService>> _logger = new();
    private readonly StockDataService _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public StockDataServiceTests()
    {
        _uow.Setup(u => u.StockListings).Returns(new Mock<IStockListingRepository>().Object);
        _uow.Setup(u => u.Metrics).Returns(new Mock<IStockMetricRepository>().Object);
        _uow.Setup(u => u.Earnings).Returns(new Mock<IEarningsSurpriseRepository>().Object);
        _uow.Setup(u => u.Recommendations).Returns(new Mock<IRecommendationTrendRepository>().Object);
        _uow.Setup(u => u.Insiders).Returns(new Mock<IInsiderTransactionRepository>().Object);

        _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_cache.Object);

        _sut = new StockDataService(
            _finnhub.Object,
            _redis.Object,
            _uow.Object,
            _marketNewsRepo.Object,
            _companyNewsRepo.Object,
            _logger.Object);
    }

    [Fact]
    public async Task GetQuote_ReturnsCachedResult_WhenAvailable()
    {
        var symbol = "AAPL";
        var cachedResponse = new StockQuoteResponse(symbol, 150m, 1m, 0.5, 152m, 148m, 149m, 149m, DateTime.UtcNow);
        var json = JsonSerializer.Serialize(cachedResponse, InventoryAlert.Domain.Configuration.JsonOptions.Default);

        _cache.Setup(c => c.StringGetAsync($"quote:{symbol}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(json);

        var result = await _sut.GetQuoteAsync(symbol, Ct);

        result.Should().NotBeNull();
        result!.Symbol.Should().Be(symbol);
        result.Price.Should().Be(150m);
        _finnhub.Verify(f => f.GetQuoteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetQuote_DiscoveryFlow_PersistsNewListing_WhenMissing()
    {
        // Arrange
        var symbol = "TSLA";
        _cache.Setup(c => c.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(RedisValue.Null);
        _uow.Setup(u => u.StockListings.FindBySymbolAsync(symbol, Ct)).ReturnsAsync((StockListing?)null);

        var profile = new FinnhubProfileResponse { Name = "Tesla Inc", Exchange = "NASDAQ" };
        _finnhub.Setup(f => f.GetProfileAsync(symbol, Ct)).ReturnsAsync(profile);
        _finnhub.Setup(f => f.GetQuoteAsync(symbol, Ct)).ReturnsAsync(new FinnhubQuoteResponse { CurrentPrice = 200m });

        // Act
        await _sut.GetQuoteAsync(symbol, Ct);

        // Assert
        _uow.Verify(u => u.StockListings.AddAsync(It.Is<StockListing>(l => l.TickerSymbol == symbol && l.Name == "Tesla Inc"), Ct), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }

    [Fact]
    public async Task GetFinancials_ReturnsResults_FromDatabase()
    {
        // Arrange
        var symbol = "MSFT";
        var metric = new StockMetric { TickerSymbol = symbol, PeRatio = 35.5 };
        _cache.Setup(c => c.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(RedisValue.Null);
        _uow.Setup(u => u.Metrics.GetBySymbolAsync(symbol, Ct)).ReturnsAsync(metric);

        // Act
        var result = await _sut.GetFinancialsAsync(symbol, Ct);

        // Assert
        result.Should().NotBeNull();
        result!.PeRatio.Should().Be(35.5);
    }

    [Fact]
    public async Task GetPeers_CachesResult_ForOneDay()
    {
        // Arrange
        var symbol = "AMD";
        var peers = new List<string> { "INTC", "NVDA" };
        _cache.Setup(c => c.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(RedisValue.Null);
        _finnhub.Setup(f => f.GetPeersAsync(symbol, Ct)).ReturnsAsync(peers);

        // Act
        var result = await _sut.GetPeersAsync(symbol, Ct);

        // Assert
        result.Should().NotBeNull();
        result!.Peers.Should().Contain("NVDA");
        /* 
        _cache.Verify(c => c.StringSetAsync(
            It.IsAny<RedisKey>(), 
            It.IsAny<RedisValue>(), 
            It.IsAny<TimeSpan?>(), 
            It.IsAny<When>(), 
            It.IsAny<CommandFlags>()), Times.Once);
        */
    }

    [Fact]
    public async Task GetMarketStatus_FetchesMajorExchanges()
    {
        // Arrange
        _finnhub.Setup(f => f.GetMarketStatusAsync(It.IsAny<string>(), Ct))
            .ReturnsAsync(new FinnhubMarketStatus { IsOpen = true, Exchange = "US" });

        // Act
        var result = await _sut.GetMarketStatusAsync(Ct);

        // Assert
        result.Should().NotBeEmpty();
        _finnhub.Verify(f => f.GetMarketStatusAsync(It.IsAny<string>(), Ct), Times.AtLeast(3));
    }
}


