using FluentAssertions;
using InventoryAlert.Api.Infrastructure.Persistence.Repositories;
using InventoryAlert.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryAlert.UnitTests.Infrastructure.Persistence.Repositories;

public class GenericRepositoryTests : IDisposable
{
    private readonly InventoryDbContext _context;
    private readonly GenericRepository<Product> _sut;

    public GenericRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new InventoryDbContext(options);
        _sut = new GenericRepository<Product>(_context);
    }

    [Fact]
    public async Task AddAsync_AddsEntity()
    {
        var product = new Product { Name = "Test Product", TickerSymbol = "TEST" };
        var result = await _sut.AddAsync(product, default);
        await _context.SaveChangesAsync();

        result.Should().NotBeNull();
        _context.Products.Count().Should().Be(1);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsCorrectPage()
    {
        // Arrange
        var products = Enumerable.Range(1, 10).Select(i => new Product { Name = $"Product {i}", TickerSymbol = $"P{i}" });
        await _sut.AddRangeAsync(products, default);
        await _context.SaveChangesAsync();

        // Act
        var (items, total) = await _sut.GetPagedAsync(skip: 2, take: 3, default);

        // Assert
        total.Should().Be(10);
        items.Should().HaveCount(3);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEntity()
    {
        var product = new Product { Name = "Old Name", TickerSymbol = "OLD" };
        await _sut.AddAsync(product, default);
        await _context.SaveChangesAsync();

        product.Name = "New Name";
        await _sut.UpdateAsync(product);
        await _context.SaveChangesAsync();

        var updated = await _sut.GetByIdAsync(product.Id, default);
        updated!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntity()
    {
        var product = new Product { Name = "Ghost", TickerSymbol = "GHOST" };
        await _sut.AddAsync(product, default);
        await _context.SaveChangesAsync();

        await _sut.DeleteAsync(product);
        await _context.SaveChangesAsync();

        _context.Products.Count().Should().Be(0);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
