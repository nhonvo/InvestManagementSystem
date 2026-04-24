using InventoryAlert.Domain.DTOs;

namespace InventoryAlert.Domain.Interfaces;

public interface IWatchlistService
{
    /// <summary>List the user's watchlist with current price data merged in.</summary>
    Task<IEnumerable<PortfolioPositionResponse>> GetWatchlistAsync(string userId, CancellationToken ct);

    /// <summary>Get a single watchlist entry with detailed data.</summary>
    Task<PortfolioPositionResponse?> GetWatchlistItemAsync(string symbol, string userId, CancellationToken ct);

    /// <summary>Add a ticker to the user's watchlist. Resolves symbol via DB-first + Finnhub fallback.</summary>
    Task<PortfolioPositionResponse?> AddToWatchlistAsync(string symbol, string userId, CancellationToken ct);

    /// <summary>Remove a ticker from the watchlist.</summary>
    Task RemoveFromWatchlistAsync(string symbol, string userId, CancellationToken ct);
}
