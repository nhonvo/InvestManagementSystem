using FluentAssertions;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace InventoryAlert.UnitTests.Infrastructure.Persistence.Repositories;

public class UnitOfWorkTests
{
    private readonly AppDbContext _db;
    private readonly UnitOfWork _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public UnitOfWorkTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new AppDbContext(options);
        _sut = new UnitOfWork(_db);
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsChanges()
    {
        _db.StockListings.Add(new StockListing { TickerSymbol = "AAPL", Name = "Apple" });
        await _sut.SaveChangesAsync(Ct);

        _db.StockListings.Count().Should().Be(1);
    }

    [Fact]
    public async Task ExecuteTransactionAsync_CommitsChanges_OnSuccess()
    {
        await _sut.ExecuteTransactionAsync(async () =>
        {
            _db.StockListings.Add(new StockListing { TickerSymbol = "AAPL", Name = "Apple" });
            await Task.CompletedTask;
        }, Ct);

        _db.StockListings.Count().Should().Be(1);
    }

    [Fact]
    public async Task ExecuteTransactionAsync_RollsBack_OnFailure()
    {
        var action = async () =>
        {
            await _sut.ExecuteTransactionAsync(async () =>
            {
                _db.StockListings.Add(new StockListing { TickerSymbol = "AAPL", Name = "Apple" });
                throw new Exception("Boom");
            }, Ct);
        };

        await action.Should().ThrowAsync<Exception>()
            .WithMessage("Boom");
    }
}
