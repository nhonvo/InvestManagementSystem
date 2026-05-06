using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;

namespace InventoryAlert.IntegrationTests.Tests.Services;

[Collection("IntegrationTests")]
[Trait("Category", "Jobs")]
public class PortfolioServiceTests : IAsyncLifetime
{
    private readonly TestFixture _fixture;
    private readonly IServiceProvider _provider;

    public PortfolioServiceTests(TestFixture fixture)
    {
        _fixture = fixture;
        // PortfolioService is in Api project, use Api provider
        _provider = SetupDI.BuildApiServiceProvider(_fixture.Configuration, _fixture.LoggerProvider);
    }

    public async Task InitializeAsync() => await _fixture.ResetStateAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    
    public async Task RecordTradeAsync_UpdatesHoldings_AndReturnsCorrectResponse()
    {
        // Arrange
        var unitOfWork = _provider.GetRequiredService<IUnitOfWork>();
        var portfolioService = _provider.GetRequiredService<IPortfolioService>();
        var ct = CancellationToken.None;

        var userId = Guid.NewGuid();
        var symbol = "AAPL";
        
        await unitOfWork.Users.AddAsync(new User { Id = userId, Username = "portfoliouser", Email = "p@test.com", PasswordHash = "..." }, ct);
        await unitOfWork.StockListings.AddAsync(new StockListing { TickerSymbol = symbol, Name = "Apple" }, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var request = new TradeRequest(TradeType.Buy, 10, 150m, "First trade");

        // Act
        var response = await portfolioService.RecordTradeAsync(symbol, request, userId.ToString(), ct);

        // Assert
        response.Symbol.Should().Be(symbol);
        response.HoldingsCount.Should().Be(10);
        
        var trades = await unitOfWork.Trades.GetByUserAndSymbolAsync(userId, symbol, ct);
        trades.Should().ContainSingle(t => t.Quantity == 10 && t.UnitPrice == 150m);
    }

    [Fact]
    
    public async Task RecordTradeAsync_SellWithInsufficientHoldings_ThrowsException()
    {
        // Arrange
        var portfolioService = _provider.GetRequiredService<IPortfolioService>();
        var ct = CancellationToken.None;
        var userId = Guid.NewGuid().ToString();

        var request = new TradeRequest(TradeType.Sell, 100, 150m, "Short sell attempt");

        // Act & Assert
        await FluentActions.Invoking(() => portfolioService.RecordTradeAsync("AAPL", request, userId, ct))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Insufficient holdings*");
    }
}
