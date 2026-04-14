using InventoryAlert.Domain.External.Finnhub;

namespace InventoryAlert.Domain.Interfaces;

public interface IFinnhubClient
{
    Task<FinnhubQuoteResponse?> GetQuoteAsync(string symbol, CancellationToken ct = default);
    Task<FinnhubProfileResponse?> GetProfileAsync(string symbol, CancellationToken ct = default);
    Task<List<FinnhubNewsItem>> GetCompanyNewsAsync(string symbol, string from, string to, CancellationToken ct = default);
    Task<List<FinnhubRecommendation>> GetRecommendationsAsync(string symbol, CancellationToken ct = default);
    Task<List<FinnhubEarnings>> GetEarningsAsync(string symbol, CancellationToken ct = default);
    Task<List<string>> GetPeersAsync(string symbol, CancellationToken ct = default);
    Task<List<FinnhubNewsItem>> GetMarketNewsAsync(string category, CancellationToken ct = default);
    Task<FinnhubMarketStatus?> GetMarketStatusAsync(string exchange, CancellationToken ct = default);
    Task<List<FinnhubHoliday>> GetMarketHolidaysAsync(string exchange, CancellationToken ct = default);
    Task<FinnhubEarningsCalendar?> GetEarningsCalendarAsync(string from, string to, CancellationToken ct = default);
    Task<FinnhubSymbolSearch?> SearchSymbolsAsync(string query, CancellationToken ct = default);

    // New methods
    Task<FinnhubMetricsResponse?> GetMetricsAsync(string symbol, CancellationToken ct = default);
    Task<FinnhubInsiderResponse?> GetInsidersAsync(string symbol, CancellationToken ct = default);
    Task<FinnhubIpoCalendar?> GetIpoCalendarAsync(string from, string to, CancellationToken ct = default);
}
