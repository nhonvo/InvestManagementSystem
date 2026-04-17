using InventoryAlert.Domain.DTOs;

namespace InventoryAlert.Domain.Interfaces;

public interface IStockDataService
{
    // Stocks
    Task<StockQuoteResponse?> GetQuoteAsync(string symbol, CancellationToken ct = default);
    Task<StockProfileResponse?> GetProfileAsync(string symbol, CancellationToken ct = default);
    Task<StockMetricResponse?> GetFinancialsAsync(string symbol, CancellationToken ct = default);
    Task<IEnumerable<EarningsSurpriseResponse>> GetEarningsAsync(string symbol, CancellationToken ct = default);
    Task<IEnumerable<RecommendationResponse>> GetRecommendationsAsync(string symbol, CancellationToken ct = default);
    Task<IEnumerable<InsiderTransactionResponse>> GetInsidersAsync(string symbol, CancellationToken ct = default);
    Task<PeersResponse?> GetPeersAsync(string symbol, CancellationToken ct = default);
    Task<IEnumerable<NewsResponse>> GetCompanyNewsAsync(string symbol, int page = 1, int pageSize = 10, CancellationToken ct = default);

    // Market
    Task<IEnumerable<MarketStatusResponse>> GetMarketStatusAsync(CancellationToken ct = default);
    Task<IEnumerable<NewsResponse>> GetMarketNewsAsync(string category, int page, int pageSize = 20, CancellationToken ct = default);
    Task<IEnumerable<MarketHolidayResponse>> GetMarketHolidaysAsync(string exchange, CancellationToken ct = default);
    Task<IEnumerable<EarningsCalendarResponse>> GetEarningsCalendarAsync(DateOnly from, DateOnly to, string? symbol = null, CancellationToken ct = default);
    Task<IEnumerable<IpoCalendarResponse>> GetIpoCalendarAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    // Search
    Task<IEnumerable<SymbolSearchResponse>> SearchSymbolsAsync(string query, CancellationToken ct = default);
}
