using InventoryAlert.Contracts.Persistence;
using InventoryAlert.Api.Application.Common.Exceptions;
using InventoryAlert.Api.Domain.Constants;
using InventoryAlert.Api.Domain.Interfaces;
using InventoryAlert.Api.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace InventoryAlert.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Coordinates atomic transactions across one or more repositories.
/// Use when an operation spans multiple DB writes that must succeed or fail together.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly InventoryDbContext _dbContext;
    public IProductRepository ProductRepository { get; }

    public UnitOfWork(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
        ProductRepository = new ProductRepository(_dbContext);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ExecuteTransactionAsync(Action action, CancellationToken cancellationToken)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            action();
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new UserFriendlyException(ErrorCode.Internal, ApplicationConstants.Messages.TransactionFailed, ex);
        }
    }

    public async Task ExecuteTransactionAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await action();
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new UserFriendlyException(ErrorCode.Internal, ApplicationConstants.Messages.TransactionFailed, ex);
        }
    }
}

