using System.Text.Json;
using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Domain.Interfaces;
using InventoryAlert.Contracts.Configuration;
using StackExchange.Redis;

namespace InventoryAlert.Api.Application.Services;

/// <summary>
/// Orchestrates stock, market, and crypto data queries.
/// Cache-first (Redis), falls back to Finnhub; DynamoDB for persisted history.
/// </summary>
public class StockDataService(
    IFinnhubClient finnhub,
    IConnectionMultiplexer redis,
    IUnitOfWork unitOfWork,
    ILogger<StockDataService> logger) : IStockDataService
{
    private readonly IFinnhubClient _finnhub = finnhub;
    private readonly IDatabase _cache = redis.GetDatabase();
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<StockDataService> _logger = logger;
    private static readonly JsonSerializerOptions _json = JsonOptions.Default;

    // ── Quote ─────────────────────────────────────────────────────────────────

    public async Task<StockQuoteResponse?> GetQuoteAsync(string symbol, CancellationToken ct = default)
    {
        var cacheKey = $"quote:{symbol}";
        var cached = await _cache.StringGetAsync(cacheKey);
        if (cached.HasValue)
            return JsonSerializer.Deserialize<StockQuoteResponse>((string)cached!, _json);

        var q = await _finnhub.GetQuoteAsync(symbol, ct);
        if (q?.CurrentPrice is null or 0)
        {
            _logger.LogWarning("[StockDataService] Finnhub returned null/zero quote for {Symbol}.", symbol);
            return null;
        }

        var result = new StockQuoteResponse(symbol, q.CurrentPrice!.Value, q.Change ?? 0,
            q.PercentChange ?? 0, q.HighPrice ?? 0, q.LowPrice ?? 0,
            q.OpenPrice ?? 0, q.PreviousClose ?? 0, q.Timestamp ?? 0);

        await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result, _json), TimeSpan.FromMinutes(1));
        return result;
    }

    // ── Profile ───────────────────────────────────────────────────────────────

    public async Task<CompanyProfileResponse?> GetProfileAsync(string symbol, CancellationToken ct = default)
    {
        var cacheKey = $"profile:{symbol}";
        var cached = await _cache.StringGetAsync(cacheKey);
        if (cached.HasValue)
            return JsonSerializer.Deserialize<CompanyProfileResponse>((string)cached!, _json);

        var profile = await _unitOfWork.CompanyProfiles.GetBySymbolAsync(symbol, ct);

        CompanyProfileResponse? result = null;
        if (profile is not null)
        {
            result = new CompanyProfileResponse(profile.Symbol, profile.Name, profile.Logo, profile.Industry,
                profile.Exchange, profile.MarketCap, profile.IpoDate, profile.WebUrl, profile.Country, profile.Currency);
        }
        else
        {
            var raw = await _finnhub.GetProfileAsync(symbol, ct);
            if (raw is not null)
            {
                result = new CompanyProfileResponse(symbol, raw.Name ?? symbol, raw.Logo, raw.Industry,
                    raw.Exchange, raw.MarketCap, DateOnly.TryParse(raw.IpoDate, out var d) ? d : null,
                    raw.WebUrl, raw.Country, raw.Currency);
            }
        }

        if (result is not null)
        {
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result, _json), TimeSpan.FromDays(7));
        }

        return result;
    }

    // ── News ──────────────────────────────────────────────────────────────────

    public async Task<List<CompanyNewsResponse>> GetCompanyNewsAsync(string symbol, int limit, string? from, string? to, CancellationToken ct = default)
    {
        var toDate = to ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
        var fromDate = from ?? DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");

        var items = await _finnhub.GetCompanyNewsAsync(symbol, fromDate, toDate, ct);
        return items
            .Take(limit)
            .Select(n => new CompanyNewsResponse(
                n.Headline ?? string.Empty, n.Summary, n.Source, n.Url, n.Image,
                DateTimeOffset.FromUnixTimeSeconds(n.Datetime).ToString("O")))
            .ToList();
    }

    // ── Recommendations ───────────────────────────────────────────────────────

    public async Task<List<RecommendationResponse>> GetRecommendationsAsync(string symbol, CancellationToken ct = default)
    {
        var items = await _finnhub.GetRecommendationsAsync(symbol, ct);
        return items.Select(r => new RecommendationResponse(
            r.Period ?? string.Empty, r.StrongBuy, r.Buy, r.Hold, r.Sell, r.StrongSell)).ToList();
    }

    // ── Earnings ──────────────────────────────────────────────────────────────

    public async Task<List<EarningsResponse>> GetEarningsAsync(string symbol, int limit, CancellationToken ct = default)
    {
        var items = await _finnhub.GetEarningsAsync(symbol, ct);
        return items.Take(limit)
            .Select(e => new EarningsResponse(
                e.Period ?? string.Empty, e.Actual, e.Estimate, e.Surprise, e.SurprisePercent))
            .ToList();
    }

    // ── Peers ─────────────────────────────────────────────────────────────────

    public async Task<List<string>> GetPeersAsync(string symbol, CancellationToken ct = default)
    {
        var cacheKey = $"peers:{symbol}";
        var cached = await _cache.StringGetAsync(cacheKey);
        if (cached.HasValue)
            return JsonSerializer.Deserialize<List<string>>((string)cached!, _json) ?? [];

        var peers = await _finnhub.GetPeersAsync(symbol, ct);
        await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(peers, _json), TimeSpan.FromHours(24));
        return peers;
    }

    // ── Market News ───────────────────────────────────────────────────────────

    public async Task<List<MarketNewsResponse>> GetMarketNewsAsync(string category, int limit, CancellationToken ct = default)
    {
        var items = await _finnhub.GetMarketNewsAsync(category, ct);
        return items.Take(limit)
            .Select(n => new MarketNewsResponse(
                n.Headline ?? string.Empty, n.Summary, n.Source, n.Url, n.Image, n.Category,
                DateTimeOffset.FromUnixTimeSeconds(n.Datetime).ToString("O")))
            .ToList();
    }

    // ── Market Status ─────────────────────────────────────────────────────────

    public async Task<MarketStatusResponse?> GetMarketStatusAsync(string exchange, CancellationToken ct = default)
    {
        var cacheKey = $"market:status:{exchange}";
        var cached = await _cache.StringGetAsync(cacheKey);
        if (cached.HasValue)
            return JsonSerializer.Deserialize<MarketStatusResponse>((string)cached!, _json);

        var raw = await _finnhub.GetMarketStatusAsync(exchange, ct);
        if (raw is null) return null;

        var result = new MarketStatusResponse(raw.Exchange ?? exchange, raw.IsOpen, raw.Session, raw.Holiday);
        await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result, _json), TimeSpan.FromMinutes(5));
        return result;
    }

    // ── Market Holidays ───────────────────────────────────────────────────────

    public async Task<List<HolidayResponse>> GetMarketHolidaysAsync(string exchange, CancellationToken ct = default)
    {
        var items = await _finnhub.GetMarketHolidaysAsync(exchange, ct);
        return items.Select(h => new HolidayResponse(h.AtDate ?? string.Empty, h.EventName ?? string.Empty, h.TradingHour)).ToList();
    }

    // ── Earnings Calendar ─────────────────────────────────────────────────────

    public async Task<List<EarningsCalendarResponse>> GetEarningsCalendarAsync(string from, string to, CancellationToken ct = default)
    {
        var cal = await _finnhub.GetEarningsCalendarAsync(from, to, ct);
        return cal?.Items
            .Select(e => new EarningsCalendarResponse(e.Symbol ?? string.Empty, e.Date ?? string.Empty, e.EpsEstimate, e.RevenueEstimate))
            .ToList() ?? [];
    }

    // ── Symbol Search ─────────────────────────────────────────────────────────

    // TODO: Optimize by bulk fetching multiple symbols if the UI requested many items.
    public async Task<List<SymbolSearchResponse>> SearchSymbolsAsync(string query, string? type, CancellationToken ct = default)
    {
        var result = await _finnhub.SearchSymbolsAsync(query, ct);
        var items = result?.Result ?? [];
        if (!string.IsNullOrEmpty(type))
            items = items.Where(s => string.Equals(s.Type, type, StringComparison.OrdinalIgnoreCase)).ToList();

        return items.Select(s => new SymbolSearchResponse(
            s.Symbol ?? string.Empty, s.Description ?? string.Empty, s.Type ?? string.Empty, s.DisplaySymbol)).ToList();
    }

    // ── Crypto ────────────────────────────────────────────────────────────────

    public async Task<List<CryptoExchangeResponse>> GetCryptoExchangesAsync(CancellationToken ct = default)
    {
        var cacheKey = "crypto:exchanges";
        var cached = await _cache.StringGetAsync(cacheKey);
        if (cached.HasValue)
            return JsonSerializer.Deserialize<List<CryptoExchangeResponse>>((string)cached!, _json) ?? [];

        var exchanges = await _finnhub.GetCryptoExchangesAsync(ct);
        var result = exchanges.Select(e => new CryptoExchangeResponse(e)).ToList();
        await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result, _json), TimeSpan.FromHours(1));
        return result;
    }

    public async Task<List<CryptoSymbolResponse>> GetCryptoSymbolsAsync(string exchange, CancellationToken ct = default)
    {
        var cacheKey = $"crypto:symbols:{exchange}";
        var cached = await _cache.StringGetAsync(cacheKey);
        if (cached.HasValue)
            return JsonSerializer.Deserialize<List<CryptoSymbolResponse>>((string)cached!, _json) ?? [];

        var symbols = await _finnhub.GetCryptoSymbolsAsync(exchange, ct);
        var result = symbols.Select(s => new CryptoSymbolResponse(
            s.Symbol ?? string.Empty, s.DisplaySymbol ?? string.Empty, s.Description ?? string.Empty)).ToList();
        await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result, _json), TimeSpan.FromHours(1));
        return result;
    }

    public Task<StockQuoteResponse?> GetCryptoQuoteAsync(string symbol, CancellationToken ct = default)
        => GetQuoteAsync(symbol, ct); // Same Finnhub /quote endpoint works for crypto pairs
}
