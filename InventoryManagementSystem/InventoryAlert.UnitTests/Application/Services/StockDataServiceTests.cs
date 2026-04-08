using FluentAssertions;
using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Application.Services;
using InventoryAlert.Api.Domain.Interfaces;
using InventoryAlert.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using System.Text.Json;
using Xunit;

namespace InventoryAlert.UnitTests.Application.Services;

public class StockDataServiceTests
{
    private readonly Mock<IFinnhubClient> _finnhub = new();
    private readonly Mock<IConnectionMultiplexer> _redis = new();
    private readonly Mock<IDatabase> _cache = new();
    private readonly Mock<ILogger<StockDataService>> _logger = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly InventoryDbContext _db;
    private readonly StockDataService _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public StockDataServiceTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new InventoryDbContext(options);
        
        _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_cache.Object);

        var mockProfileRepo = new Mock<ICompanyProfileRepository>();
        mockProfileRepo.Setup(x => x.GetBySymbolAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((sym, ct) => _db.CompanyProfiles.FirstOrDefaultAsync(p => p.Symbol == sym, ct));
        
        _uow.Setup(x => x.CompanyProfiles).Returns(mockProfileRepo.Object);

        _sut = new StockDataService(_finnhub.Object, _redis.Object, _uow.Object, _logger.Object);
    }

    [Fact]
    public async Task GetQuote_ReturnsCachedResult_WhenAvailable()
    {
        var symbol = "AAPL";
        var cachedResponse = new StockQuoteResponse(symbol, 150m, 1m, 0.5m, 152m, 148m, 149m, 149m, 123456789L);
        var json = JsonSerializer.Serialize(cachedResponse, InventoryAlert.Contracts.Configuration.JsonOptions.Default);
        
        _cache.Setup(c => c.StringGetAsync($"quote:{symbol}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(json);

        var result = await _sut.GetQuoteAsync(symbol, Ct);

        result.Should().NotBeNull();
        result!.Symbol.Should().Be(symbol);
        result.Price.Should().Be(150m);
        _finnhub.Verify(f => f.GetQuoteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetQuote_FetchesFromFinnhub_WhenCacheMiss()
    {
        var symbol = "AAPL";
        _cache.Setup(c => c.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var finnhubResponse = new FinnhubQuoteResponse { CurrentPrice = 160m, Change = 2m, PercentChange = 1.0m };
        _finnhub.Setup(f => f.GetQuoteAsync(symbol, Ct)).ReturnsAsync(finnhubResponse);

        var result = await _sut.GetQuoteAsync(symbol, Ct);

        result.Should().NotBeNull();
        result!.Price.Should().Be(160m);
        // _cache.Verify(c => c.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task GetPeers_ReturnsCachedResult_WhenAvailable()
    {
        var symbol = "AAPL";
        var peers = new List<string> { "MSFT", "GOOGL" };
        var json = JsonSerializer.Serialize(peers, InventoryAlert.Contracts.Configuration.JsonOptions.Default);
        _cache.Setup(c => c.StringGetAsync($"peers:{symbol}", It.IsAny<CommandFlags>())).ReturnsAsync(json);

        var result = await _sut.GetPeersAsync(symbol, Ct);

        result.Should().BeEquivalentTo(peers);
    }

    [Fact]
    public async Task GetMarketStatus_ReturnsCachedResult_WhenAvailable()
    {
        var exchange = "US";
        var status = new MarketStatusResponse(exchange, true, "regular", "");
        var json = JsonSerializer.Serialize(status, InventoryAlert.Contracts.Configuration.JsonOptions.Default);
        _cache.Setup(c => c.StringGetAsync($"market:status:{exchange}", It.IsAny<CommandFlags>())).ReturnsAsync(json);

        var result = await _sut.GetMarketStatusAsync(exchange, Ct);

        result.Should().NotBeNull();
        result!.IsOpen.Should().BeTrue();
    }

    [Fact]
    public async Task GetCryptoExchanges_ReturnsCachedResult_WhenAvailable()
    {
        var exchanges = new List<CryptoExchangeResponse> { new("BINANCE"), new("BITFINEX") };
        var json = JsonSerializer.Serialize(exchanges, InventoryAlert.Contracts.Configuration.JsonOptions.Default);
        _cache.Setup(c => c.StringGetAsync("crypto:exchanges", It.IsAny<CommandFlags>())).ReturnsAsync(json);

        var result = await _sut.GetCryptoExchangesAsync(Ct);

        result.Should().HaveCount(2);
        result[0].Exchange.Should().Be("BINANCE");
    }

    [Fact]
    public async Task GetProfile_ReturnsDbResult_WhenExists()
    {
        var symbol = "AAPL";
        _db.CompanyProfiles.Add(new InventoryAlert.Contracts.Entities.CompanyProfile { Symbol = symbol, Name = "Apple Inc" });
        await _db.SaveChangesAsync(Ct);

        var result = await _sut.GetProfileAsync(symbol, Ct);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Apple Inc");
        _finnhub.Verify(f => f.GetProfileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetProfile_FetchesFromFinnhub_WhenNotInDb()
    {
        var symbol = "AAPL";
        var finnhubResponse = new FinnhubProfileResponse { Name = "Apple Inc", Logo = "logo-url" };
        _finnhub.Setup(f => f.GetProfileAsync(symbol, Ct)).ReturnsAsync(finnhubResponse);

        var result = await _sut.GetProfileAsync(symbol, Ct);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Apple Inc");
        result.Logo.Should().Be("logo-url");
    }

    [Fact]
    public async Task SearchSymbols_FetchesFromFinnhub()
    {
        var query = "AAPL";
        var finnhubResponse = new FinnhubSymbolSearch { Result = [new FinnhubSymbolItem { Symbol = "AAPL", Description = "Apple" }] };
        _finnhub.Setup(f => f.SearchSymbolsAsync(query, Ct)).ReturnsAsync(finnhubResponse);

        var result = await _sut.SearchSymbolsAsync(query, null, Ct);

        result.Should().HaveCount(1);
        result[0].Symbol.Should().Be("AAPL");
    }

    [Fact]
    public async Task GetCompanyNews_FetchesFromFinnhub()
    {
        var symbol = "AAPL";
        var finnhubResponse = new List<FinnhubNewsItem> { new() { Headline = "News" } };
        _finnhub.Setup(f => f.GetCompanyNewsAsync(symbol, It.IsAny<string>(), It.IsAny<string>(), Ct)).ReturnsAsync(finnhubResponse);

        var result = await _sut.GetCompanyNewsAsync(symbol, 10, "2024-01-01", "2024-01-02", Ct);

        result.Should().HaveCount(1);
        result[0].Headline.Should().Be("News");
    }

    [Fact]
    public async Task GetEarnings_FetchesFromFinnhub()
    {
        var symbol = "AAPL";
        var finnhubResponse = new List<FinnhubEarnings> { new() { Period = "2024Q1", Actual = 1.5m } };
        _finnhub.Setup(f => f.GetEarningsAsync(symbol, Ct)).ReturnsAsync(finnhubResponse);

        var result = await _sut.GetEarningsAsync(symbol, 4, Ct);

        result.Should().HaveCount(1);
        result[0].Period.Should().Be("2024Q1");
    }
}
