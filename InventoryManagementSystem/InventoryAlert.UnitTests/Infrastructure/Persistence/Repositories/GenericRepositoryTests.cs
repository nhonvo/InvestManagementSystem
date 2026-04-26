using FluentAssertions;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryAlert.UnitTests.Infrastructure.Persistence.Repositories;

public class GenericRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly GenericRepository<StockListing> _sut;

    public GenericRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _sut = new GenericRepository<StockListing>(_context);
    }

    [Fact]
    public async Task AddAsync_AddsEntity()
    {
        var listing = new StockListing { Name = "Test Listing", TickerSymbol = "TEST" };
        var result = await _sut.AddAsync(listing, default);
        await _context.SaveChangesAsync();

        result.Should().NotBeNull();
        _context.StockListings.Count().Should().Be(1);
    }

    [Fact]
    public async Task AddRangeAsync_AddsEntities()
    {
        var listings = new List<StockListing>
        {
            new() { Name = "Test 1", TickerSymbol = "T1" },
            new() { Name = "Test 2", TickerSymbol = "T2" }
        };

        await _sut.AddRangeAsync(listings, default);
        await _context.SaveChangesAsync();

        _context.StockListings.Count().Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsCorrectCount()
    {
        // Arrange
        var items = Enumerable.Range(1, 10).Select(i => new StockListing { Name = $"Listing {i}", TickerSymbol = $"L{i}" });
        foreach (var item in items) await _sut.AddAsync(item, default);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync(default);

        // Assert
        result.Should().HaveCount(10);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEntity()
    {
        var listing = new StockListing { Name = "Old Name", TickerSymbol = "OLD" };
        await _sut.AddAsync(listing, default);
        await _context.SaveChangesAsync();

        listing.Name = "New Name";
        await _sut.UpdateAsync(listing, default);
        await _context.SaveChangesAsync();

        var updated = await _sut.GetByIdAsync(listing.Id, default);
        updated!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntity()
    {
        var listing = new StockListing { Name = "Ghost", TickerSymbol = "GHOST" };
        await _sut.AddAsync(listing, default);
        await _context.SaveChangesAsync();

        await _sut.DeleteAsync(listing, default);
        await _context.SaveChangesAsync();

        _context.StockListings.Count().Should().Be(0);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
