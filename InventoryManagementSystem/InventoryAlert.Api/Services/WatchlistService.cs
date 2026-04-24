using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;

namespace InventoryAlert.Api.Services;

public class WatchlistService(
    IUnitOfWork unitOfWork,
    IStockDataService stockDataService,
    ILogger<WatchlistService> logger) : IWatchlistService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IStockDataService _stockDataService = stockDataService;
    private readonly ILogger<WatchlistService> _logger = logger;

    public async Task<IEnumerable<PortfolioPositionResponse>> GetWatchlistAsync(string userId, CancellationToken ct)
    {
        var items = await _unitOfWork.WatchlistItems.GetByUserIdAsync(userId, ct);
        var results = new List<PortfolioPositionResponse>();

        foreach (var item in items)
        {
            var position = await BuildPositionResponseAsync(item.TickerSymbol, ct);
            if (position != null)
                results.Add(position);
        }

        return results;
    }

    public async Task<PortfolioPositionResponse?> GetWatchlistItemAsync(string symbol, string userId, CancellationToken ct)
    {
        var item = await _unitOfWork.WatchlistItems.GetByUserAndSymbolAsync(userId, symbol, ct);
        if (item == null) return null;

        return await BuildPositionResponseAsync(symbol, ct);
    }

    public async Task<PortfolioPositionResponse?> AddToWatchlistAsync(string symbol, string userId, CancellationToken ct)
    {
        // Guard: already on watchlist
        var existing = await _unitOfWork.WatchlistItems.GetByUserAndSymbolAsync(userId, symbol, ct);
        if (existing != null)
        {
            return null;
        }

        // DB-first symbol resolution
        var listing = await _unitOfWork.StockListings.FindBySymbolAsync(symbol, ct);
        if (listing == null)
        {
            // Finnhub fallback — GetProfileAsync internally persists via EnsureListingAsync
            var profile = await _stockDataService.GetProfileAsync(symbol, ct);
            if (profile == null)
                throw new KeyNotFoundException($"Symbol '{symbol}' is not recognized.");
        }

        var watchlistItem = new WatchlistItem
        {
            UserId = Guid.Parse(userId),
            TickerSymbol = symbol.ToUpperInvariant(),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.WatchlistItems.AddAsync(watchlistItem, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("[Watchlist] User {UserId} added {Symbol}", userId, symbol);

        return await BuildPositionResponseAsync(symbol, ct)
               ?? throw new InvalidOperationException("Failed to build position response after watchlist add.");
    }

    public async Task RemoveFromWatchlistAsync(string symbol, string userId, CancellationToken ct)
    {
        var item = await _unitOfWork.WatchlistItems.GetByUserAndSymbolAsync(userId, symbol, ct);
        if (item == null)
            throw new KeyNotFoundException($"Symbol '{symbol}' is not on your watchlist.");

        await _unitOfWork.WatchlistItems.DeleteAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("[Watchlist] User {UserId} removed {Symbol}", userId, symbol);
    }

    private async Task<PortfolioPositionResponse?> BuildPositionResponseAsync(string symbol, CancellationToken ct)
    {
        var listing = await _unitOfWork.StockListings.FindBySymbolAsync(symbol, ct);
        if (listing == null) return null;

        var quote = await _stockDataService.GetQuoteAsync(symbol, ct);
        var currentPrice = quote?.Price ?? 0;

        // Watchlist positions are watch-only (no trades), so holdings = 0
        return new PortfolioPositionResponse(
            StockId: listing.Id,
            Symbol: symbol,
            Name: listing.Name,
            Exchange: listing.Exchange,
            Logo: listing.Logo,
            HoldingsCount: 0,
            AveragePrice: 0,
            CurrentPrice: currentPrice,
            MarketValue: 0,
            TotalCost: 0,
            TotalReturn: 0,
            TotalReturnPercent: 0,
            PriceChange: quote?.Change ?? 0,
            PriceChangePercent: (decimal)(quote?.ChangePercent ?? 0),
            Industry: listing.Industry);
    }
}
