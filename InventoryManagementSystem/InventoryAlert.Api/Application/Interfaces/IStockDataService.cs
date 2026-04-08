using InventoryAlert.Api.Application.DTOs;

namespace InventoryAlert.Api.Application.Interfaces;

public interface IStockDataService
{
    Task<StockQuoteResponse?> GetQuoteAsync(string symbol, CancellationToken ct = default);
    Task<CompanyProfileResponse?> GetProfileAsync(string symbol, CancellationToken ct = default);
    Task<List<CompanyNewsResponse>> GetCompanyNewsAsync(string symbol, int limit, string? from, string? to, CancellationToken ct = default);
    Task<List<RecommendationResponse>> GetRecommendationsAsync(string symbol, CancellationToken ct = default);
    Task<List<EarningsResponse>> GetEarningsAsync(string symbol, int limit, CancellationToken ct = default);
    Task<List<string>> GetPeersAsync(string symbol, CancellationToken ct = default);

    // Market
    Task<List<MarketNewsResponse>> GetMarketNewsAsync(string category, int limit, CancellationToken ct = default);
    Task<MarketStatusResponse?> GetMarketStatusAsync(string exchange, CancellationToken ct = default);
    Task<List<HolidayResponse>> GetMarketHolidaysAsync(string exchange, CancellationToken ct = default);
    Task<List<EarningsCalendarResponse>> GetEarningsCalendarAsync(string from, string to, CancellationToken ct = default);

    // Symbol search
    Task<List<SymbolSearchResponse>> SearchSymbolsAsync(string query, string? type, CancellationToken ct = default);

    // Crypto
    Task<List<CryptoExchangeResponse>> GetCryptoExchangesAsync(CancellationToken ct = default);
    Task<List<CryptoSymbolResponse>> GetCryptoSymbolsAsync(string exchange, CancellationToken ct = default);
    Task<StockQuoteResponse?> GetCryptoQuoteAsync(string symbol, CancellationToken ct = default);
}
