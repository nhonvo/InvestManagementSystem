using FluentAssertions;
using InventoryAlert.Domain.Entities.Dynamodb;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.External.Finnhub;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Worker.Configuration;
using InventoryAlert.Worker.ScheduledJobs;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Worker.ScheduledJobs;

public class NewsSyncJobTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IFinnhubClient> _finnhubMock = new();
    private readonly Mock<ICompanyNewsDynamoRepository> _companyNewsRepoMock = new();
    private readonly Mock<IMarketNewsDynamoRepository> _marketNewsRepoMock = new();
    private readonly Mock<ILogger<NewsSyncJob>> _loggerMock = new();
    private readonly NewsSyncJob _sut;

    public NewsSyncJobTests()
    {
        var settings = new WorkerSettings { MaxDegreeOfParallelism = 5 };
        _uowMock.Setup(u => u.StockListings).Returns(new Mock<IStockListingRepository>().Object);
        _sut = new NewsSyncJob(
            _uowMock.Object, 
            _finnhubMock.Object, 
            _companyNewsRepoMock.Object, 
            _marketNewsRepoMock.Object, 
            settings,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_SyncsMarketAndCompanyNews()
    {
        // Arrange
        var generalNews = new List<FinnhubNewsItem> { new() { Id = 1, Headline = "General", Datetime = 1712217600 } };
        _finnhubMock.Setup(f => f.GetMarketNewsAsync("general", It.IsAny<CancellationToken>())).ReturnsAsync(generalNews);
        _finnhubMock.Setup(f => f.GetMarketNewsAsync(It.Is<string>(s => s != "general"), It.IsAny<CancellationToken>())).ReturnsAsync(new List<FinnhubNewsItem>());

        var listing = new StockListing { TickerSymbol = "AAPL" };
        _uowMock.Setup(u => u.StockListings.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<StockListing> { listing });
        
        var companyNews = new List<FinnhubNewsItem> { new() { Id = 3, Headline = "Apple News", Datetime = 1712217602 } };
        _finnhubMock.Setup(f => f.GetCompanyNewsAsync("AAPL", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(companyNews);

        // Act
        var result = await _sut.ExecuteAsync(CancellationToken.None);

        // Assert
        result.Status.Should().Be(InventoryAlert.Worker.Models.JobStatus.Success);
        _marketNewsRepoMock.Verify(r => r.BatchSaveAsync(It.IsAny<IEnumerable<MarketNewsDynamoEntry>>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _companyNewsRepoMock.Verify(r => r.BatchSaveAsync(It.IsAny<IEnumerable<CompanyNewsDynamoEntry>>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_Continues_WhenApiFailsForOneSymbol()
    {
        // Arrange
        _finnhubMock.Setup(f => f.GetMarketNewsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<FinnhubNewsItem>());

        var listings = new List<StockListing> 
        { 
            new() { TickerSymbol = "AAPL" },
            new() { TickerSymbol = "MSFT" }
        };
        _uowMock.Setup(u => u.StockListings.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(listings);
        
        _finnhubMock.Setup(f => f.GetCompanyNewsAsync("AAPL", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API Error"));
        
        _finnhubMock.Setup(f => f.GetCompanyNewsAsync("MSFT", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FinnhubNewsItem> { new() { Id = 4, Headline = "MSFT News" } });

        // Act
        var result = await _sut.ExecuteAsync(CancellationToken.None);

        // Assert
        result.Status.Should().Be(InventoryAlert.Worker.Models.JobStatus.Success);
        _companyNewsRepoMock.Verify(r => r.BatchSaveAsync(It.Is<IEnumerable<CompanyNewsDynamoEntry>>(e => e.Any(x => x.Symbol == "MSFT")), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
