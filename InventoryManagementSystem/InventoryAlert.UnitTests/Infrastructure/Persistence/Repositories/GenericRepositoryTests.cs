using FluentAssertions;
using InventoryAlert.Contracts.Persistence;
using InventoryAlert.Api.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryAlert.UnitTests.Infrastructure.Persistence.Repositories;

public class GenericRepositoryTests : IDisposable
{
    private readonly InventoryDbContext _dbContext;
    private readonly GenericRepository<Product> _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public GenericRepositoryTests()
    {
        // Each test gets its own isolated in-memory database
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new InventoryDbContext(options);
        _sut = new GenericRepository<Product>(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    // ════════════════════════════════════════════════════════════════
    // AddAsync
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AddAsync_PersistsEntity_AndReturnsIt()
    {
        var product = BuildProduct(name: "Apple", ticker: "AAPL");

        var returned = await _sut.AddAsync(product, Ct);
        await _dbContext.SaveChangesAsync(Ct);

        returned.Should().NotBeNull();
        returned.Name.Should().Be("Apple");
        returned.TickerSymbol.Should().Be("AAPL");

        var inDb = await _dbContext.Products.FirstOrDefaultAsync(p => p.Name == "Apple", Ct);
        inDb.Should().NotBeNull();
    }

    // ════════════════════════════════════════════════════════════════
    // AddRangeAsync
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AddRangeAsync_AddsAllEntities()
    {
        var products = new[]
        {
            BuildProduct(name: "Alpha", ticker: "ALP"),
            BuildProduct(name: "Beta",  ticker: "BET"),
            BuildProduct(name: "Gamma", ticker: "GAM")
        };

        await _sut.AddRangeAsync(products, Ct);
        await _dbContext.SaveChangesAsync(Ct);

        var count = await _dbContext.Products.CountAsync(Ct);
        count.Should().Be(3);
    }

    // ════════════════════════════════════════════════════════════════
    // GetByIdAsync
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByIdAsync_ReturnsEntity_WhenExists()
    {
        var seeded = BuildProduct(name: "Google", ticker: "GOOGL");
        _dbContext.Products.Add(seeded);
        await _dbContext.SaveChangesAsync(Ct);

        var result = await _sut.GetByIdAsync(seeded.Id, Ct);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Google");
        result.TickerSymbol.Should().Be("GOOGL");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _sut.GetByIdAsync(999, Ct);

        result.Should().BeNull();
    }

    // ════════════════════════════════════════════════════════════════
    // GetAllAsync
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        _dbContext.Products.AddRange(
            BuildProduct(name: "P1"),
            BuildProduct(name: "P2"),
            BuildProduct(name: "P3"));
        await _dbContext.SaveChangesAsync(Ct);

        var result = await _sut.GetAllAsync(Ct);

        result.Should().HaveCount(3);
    }

    // ════════════════════════════════════════════════════════════════
    // UpdateAsync
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateAsync_MutatesEntity()
    {
        var product = BuildProduct(name: "Before");
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(Ct);

        product.Name = "After";
        await _sut.UpdateAsync(product);
        await _dbContext.SaveChangesAsync(Ct);

        var inDb = await _dbContext.Products.FindAsync([product.Id], Ct);
        inDb!.Name.Should().Be("After");
    }

    // ════════════════════════════════════════════════════════════════
    // DeleteAsync
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteAsync_RemovesEntity()
    {
        var product = BuildProduct(name: "ToDelete");
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(Ct);

        await _sut.DeleteAsync(product);
        await _dbContext.SaveChangesAsync(Ct);

        var inDb = await _dbContext.Products.FindAsync([product.Id], Ct);
        inDb.Should().BeNull();
    }

    // ════════════════════════════════════════════════════════════════
    // UpdateRangeAsync
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateRangeAsync_UpdatesAllEntities()
    {
        var products = new[]
        {
            BuildProduct(name: "Old1", currentPrice: 100m),
            BuildProduct(name: "Old2", currentPrice: 200m)
        };
        _dbContext.Products.AddRange(products);
        await _dbContext.SaveChangesAsync(Ct);

        foreach (var p in products)
            p.CurrentPrice += 50m;

        await _sut.UpdateRangeAsync(products);
        await _dbContext.SaveChangesAsync(Ct);

        var updated = await _dbContext.Products.ToListAsync(Ct);
        updated.Should().AllSatisfy(p =>
            p.CurrentPrice.Should().BeGreaterThan(100m));
    }

    // ════════════════════════════════════════════════════════════════
    // Private helper
    // ════════════════════════════════════════════════════════════════

    private static Product BuildProduct(
        string name = "Test",
        string ticker = "TST",
        decimal originPrice = 100m,
        decimal currentPrice = 90m,
        double threshold = 0.2,
        int stock = 10) => new()
        {
            Name = name,
            TickerSymbol = ticker,
            OriginPrice = originPrice,
            CurrentPrice = currentPrice,
            PriceAlertThreshold = threshold,
            StockCount = stock
        };
}
