using InventoryAlert.Domain.DTOs;

namespace InventoryAlert.Domain.Interfaces;

public interface IPortfolioService
{
    /// <summary>
    /// List user's positions (paged).
    /// </summary>
    Task<PagedResult<PortfolioPositionResponse>> GetPositionsPagedAsync(PortfolioQueryParams query, string userId, CancellationToken ct);

    /// <summary>
    /// Detailed position breakdown for one holding.
    /// </summary>
    Task<PortfolioPositionResponse?> GetPositionBySymbolAsync(string symbol, string userId, CancellationToken ct);

    /// <summary>
    /// Breached alert results for user's portfolio (threshold-based).
    /// </summary>
    Task<IEnumerable<PortfolioAlertResponse>> GetPortfolioAlertsAsync(string userId, CancellationToken ct);

    /// <summary>
    /// Open a new position.
    /// </summary>
    Task<PortfolioPositionResponse> OpenPositionAsync(CreatePositionRequest request, string userId, CancellationToken ct);

    /// <summary>
    /// Import multiple positions at once.
    /// </summary>
    Task BulkImportPositionsAsync(IEnumerable<CreatePositionRequest> requests, string userId, CancellationToken ct);

    /// <summary>
    /// Record a trade (buy/sell/etc.) to adjust holdings.
    /// </summary>
    Task<PortfolioPositionResponse> RecordTradeAsync(string symbol, TradeRequest request, string userId, CancellationToken ct);

    /// <summary>
    /// Remove a position from portfolio (requires active rules to be deleted first).
    /// </summary>
    Task RemovePositionAsync(string symbol, string userId, CancellationToken ct);
}
