using InventoryAlert.Api.Application.DTOs;

namespace InventoryAlert.Api.Application.Interfaces;

public interface IWatchlistService
{
    Task<List<WatchlistItemResponse>> GetUserWatchlistAsync(string userId, CancellationToken ct = default);
    Task AddToWatchlistAsync(string userId, string symbol, CancellationToken ct = default);
    Task RemoveFromWatchlistAsync(string userId, string symbol, CancellationToken ct = default);
}
