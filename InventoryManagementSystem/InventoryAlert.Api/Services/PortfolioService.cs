using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;

namespace InventoryAlert.Api.Services;

public class PortfolioService(
    IUnitOfWork unitOfWork,
    IStockDataService stockDataService,
    ILogger<PortfolioService> logger) : IPortfolioService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IStockDataService _stockDataService = stockDataService;
    private readonly ILogger<PortfolioService> _logger = logger;

    public async Task<PagedResult<PortfolioPositionResponse>> GetPositionsPagedAsync(PortfolioQueryParams query, string userId, CancellationToken ct)
    {
        var userGuid = Guid.Parse(userId);
        var watchlistItems = await _unitOfWork.WatchlistItems.GetByUserIdAsync(userId, ct);

        // Filter by search query if present
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            watchlistItems = watchlistItems.Where(x => x.TickerSymbol.Contains(query.Search, StringComparison.OrdinalIgnoreCase));
        }

        var totalItems = watchlistItems.Count();
        var pagedItems = watchlistItems
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize);

        var positions = new List<PortfolioPositionResponse>();

        foreach (var item in pagedItems)
        {
            var position = await GetPositionBySymbolAsync(item.TickerSymbol, userId, ct);
            if (position != null)
            {
                positions.Add(position);
            }
        }

        return new PagedResult<PortfolioPositionResponse>
        {
            Items = positions,
            TotalItems = totalItems,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task<PortfolioPositionResponse?> GetPositionBySymbolAsync(string symbol, string userId, CancellationToken ct)
    {
        var userGuid = Guid.Parse(userId);
        var trades = await _unitOfWork.Trades.GetByUserAndSymbolAsync(userGuid, symbol, ct);

        if (!trades.Any())
        {
            // If no trades, see if it's just on watchlist
            var watchlist = await _unitOfWork.WatchlistItems.GetByUserAndSymbolAsync(userId, symbol, ct);
            if (watchlist == null) return null;
        }

        var listing = await _unitOfWork.StockListings.FindBySymbolAsync(symbol, ct);
        if (listing == null) return null;

        var quote = await _stockDataService.GetQuoteAsync(symbol, ct);
        var currentPrice = quote?.Price ?? 0;

        var netHoldings = trades
            .Where(x => x.Type == TradeType.Buy).Sum(x => x.Quantity) -
            trades.Where(x => x.Type == TradeType.Sell).Sum(x => x.Quantity);

        var totalBuyCost = trades
            .Where(x => x.Type == TradeType.Buy)
            .Sum(x => x.Quantity * x.UnitPrice);

        var totalBuyQty = trades
            .Where(x => x.Type == TradeType.Buy)
            .Sum(x => x.Quantity);

        var averagePrice = totalBuyQty > 0 ? totalBuyCost / totalBuyQty : 0;
        var marketValue = netHoldings * currentPrice;
        var totalCost = netHoldings * averagePrice;
        var totalReturn = marketValue - totalCost;
        var totalReturnPercent = totalCost > 0 ? (double)(totalReturn / totalCost * 100) : 0;

        return new PortfolioPositionResponse(
            listing.Id,
            symbol,
            listing.Name,
            listing.Exchange,
            listing.Logo,
            netHoldings,
            averagePrice,
            currentPrice,
            marketValue,
            totalCost,
            totalReturn,
            totalReturnPercent,
            quote?.Change ?? 0,
            (decimal)(quote?.ChangePercent ?? 0),
            listing.Industry);
    }

    public async Task<IEnumerable<PortfolioAlertResponse>> GetPortfolioAlertsAsync(string userId, CancellationToken ct)
    {
        var userGuid = Guid.Parse(userId);
        var rules = await _unitOfWork.AlertRules.GetByUserIdAsync(userId, ct);
        var activeRules = rules.Where(r => r.IsActive);

        var alerts = new List<PortfolioAlertResponse>();

        foreach (var rule in activeRules)
        {
            var quote = await _stockDataService.GetQuoteAsync(rule.TickerSymbol, ct);
            if (quote == null) continue;

            bool breached = rule.Condition switch
            {
                AlertCondition.PriceAbove => quote.Price > rule.TargetValue,
                AlertCondition.PriceBelow => quote.Price < rule.TargetValue,
                _ => false // Other rules handled elsewhere or more complex
            };

            if (breached)
            {
                alerts.Add(new PortfolioAlertResponse(
                    rule.TickerSymbol,
                    quote.Price,
                    rule.TargetValue,
                    0, // percent loss calculation needs cost basis
                    DateTime.UtcNow));
            }
        }

        return alerts;
    }

    public async Task<PortfolioPositionResponse> OpenPositionAsync(CreatePositionRequest request, string userId, CancellationToken ct)
    {
        var userGuid = Guid.Parse(userId);

        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            // 1. Ensure StockListing exists (Discovery flow handled in StockDataService/Controller usually, but here as fallback)
            var listing = await _unitOfWork.StockListings.FindBySymbolAsync(request.TickerSymbol, ct);
            if (listing == null)
            {
                throw new InvalidOperationException($"Symbol {request.TickerSymbol} must be resolved before opening a position.");
            }

            // 2. Add to Watchlist (implied by position)
            var existingWatch = await _unitOfWork.WatchlistItems.GetByUserAndSymbolAsync(userId, request.TickerSymbol, ct);
            if (existingWatch == null)
            {
                await _unitOfWork.WatchlistItems.AddAsync(new WatchlistItem
                {
                    UserId = userGuid,
                    TickerSymbol = request.TickerSymbol,
                    CreatedAt = DateTime.UtcNow
                }, ct);
            }

            // 3. Record Initial Trade
            await _unitOfWork.Trades.AddAsync(new Trade
            {
                UserId = userGuid,
                TickerSymbol = request.TickerSymbol,
                Type = TradeType.Buy,
                Quantity = request.Quantity,
                UnitPrice = request.UnitPrice,
                TradedAt = request.TradedAt ?? DateTime.UtcNow
            }, ct);

            await _unitOfWork.SaveChangesAsync(ct);
        }, ct);

        return await GetPositionBySymbolAsync(request.TickerSymbol, userId, ct)
            ?? throw new InvalidOperationException("Failed to retrieve newly created position.");
    }

    public async Task BulkImportPositionsAsync(IEnumerable<CreatePositionRequest> requests, string userId, CancellationToken ct)
    {
        foreach (var req in requests)
        {
            try
            {
                await OpenPositionAsync(req, userId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import position for {Symbol}", req.TickerSymbol);
            }
        }
    }

    public async Task<PortfolioPositionResponse> RecordTradeAsync(string symbol, TradeRequest request, string userId, CancellationToken ct)
    {
        var userGuid = Guid.Parse(userId);

        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            var netHoldings = await _unitOfWork.Trades.GetNetHoldingsAsync(userGuid, symbol, ct);

            if (request.Type == TradeType.Sell && netHoldings < request.Quantity)
            {
                throw new InvalidOperationException("Insufficient holdings for sell trade.");
            }

            await _unitOfWork.Trades.AddAsync(new Trade
            {
                UserId = userGuid,
                TickerSymbol = symbol,
                Type = request.Type,
                Quantity = request.Quantity,
                UnitPrice = request.UnitPrice,
                Notes = request.Notes,
                TradedAt = DateTime.UtcNow
            }, ct);

            await _unitOfWork.SaveChangesAsync(ct);
        }, ct);

        return await GetPositionBySymbolAsync(symbol, userId, ct)
            ?? throw new InvalidOperationException("Failed to retrieve position after trade.");
    }

    public async Task RemovePositionAsync(string symbol, string userId, CancellationToken ct)
    {
        var userGuid = Guid.Parse(userId);

        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            // Guard: Check for active alert rules
            var rules = await _unitOfWork.AlertRules.GetByUserIdAsync(userId, ct);
            if (rules.Any(r => r.TickerSymbol == symbol && r.IsActive))
            {
                throw new InvalidOperationException("Cannot remove a position with active alert rules. Delete rules first.");
            }

            // 1. Remove watchlist entry (cascades or manual)
            var watch = await _unitOfWork.WatchlistItems.GetByUserAndSymbolAsync(userId, symbol, ct);
            if (watch != null)
            {
                await _unitOfWork.WatchlistItems.DeleteAsync(watch, ct);
            }

            // 2. Remove all trades
            var trades = await _unitOfWork.Trades.GetByUserAndSymbolAsync(userGuid, symbol, ct);
            foreach (var trade in trades)
            {
                await _unitOfWork.Trades.DeleteAsync(trade, ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);
        }, ct);
    }
}
