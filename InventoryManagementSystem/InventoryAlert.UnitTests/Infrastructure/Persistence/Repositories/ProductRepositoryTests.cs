using FluentAssertions;
using InventoryAlert.Contracts.Persistence;
using InventoryAlert.Contracts.Persistence.Repositories;
using InventoryAlert.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryAlert.UnitTests.Infrastructure.Persistence.Repositories;

public class ProductRepositoryTests : IDisposable
{
    private readonly InventoryDbContext _db;
    private readonly ProductRepository _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public ProductRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new InventoryDbContext(options);
        _sut = new ProductRepository(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetByTickerAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _sut.GetByTickerAsync("NONEXISTENT", Ct);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByTickerAsync_ReturnsProduct_WhenExists()
    {
        var product = ProductFixtures.BuildProduct(ticker: "AAPL");
        _db.Products.Add(product);
        await _db.SaveChangesAsync(Ct);

        var result = await _sut.GetByTickerAsync("AAPL", Ct);

        result.Should().NotBeNull();
        result!.TickerSymbol.Should().Be("AAPL");
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByName()
    {
        _db.Products.AddRange(
            ProductFixtures.BuildProduct(id: 10, name: "Apple"),
            ProductFixtures.BuildProduct(id: 11, name: "Banana"),
            ProductFixtures.BuildProduct(id: 12, name: "Grape")
        );
        await _db.SaveChangesAsync(Ct);

        var (items, total) = await _sut.GetPagedAsync(name: "a", null, null, null, 1, 10, Ct);

        total.Should().Be(2); // Banana, Grape
        items.Should().HaveCount(2);
    }
}
