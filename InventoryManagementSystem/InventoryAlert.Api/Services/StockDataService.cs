using System.Text.Json;
using InventoryAlert.Domain.Configuration;
using InventoryAlert.Domain.Constants;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using StackExchange.Redis;

namespace InventoryAlert.Api.Services;

public class StockDataService(
    IFinnhubClient finnhub,
    IConnectionMultiplexer redis,
    IUnitOfWork unitOfWork,
    IMarketNewsDynamoRepository marketNewsRepo,
    ICompanyNewsDynamoRepository companyNewsRepo,
    ILogger<StockDataService> logger) : IStockDataService
{
    private readonly IDatabase _cache = redis.GetDatabase();
    private readonly ILogger<StockDataService> _logger = logger;

    // ── Stocks Catalog ────────────────────────────────────────────────────────

    public async Task<PagedResult<StockProfileResponse>> GetCatalogAsync(int page, int pageSize, string? exchange, string? industry, CancellationToken ct = default)
    {
        var all = await unitOfWork.StockListings.GetAllAsync(ct);

        if (!string.IsNullOrWhiteSpace(exchange))
            all = all.Where(s => s.Exchange?.Equals(exchange, StringComparison.OrdinalIgnoreCase) == true);

        if (!string.IsNullOrWhiteSpace(industry))
            all = all.Where(s => s.Industry?.Contains(industry, StringComparison.OrdinalIgnoreCase) == true);

        var totalItems = all.Count();
        var paged = all.Skip((page - 1) * pageSize).Take(pageSize).Select(s => new StockProfileResponse(
            s.TickerSymbol, s.Name, s.Exchange, s.Currency, s.Country, s.Industry, s.MarketCap, s.Ipo, s.WebUrl, s.Logo));

        return new PagedResult<StockProfileResponse>
        {
            Items = paged,
            TotalItems = totalItems,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    // ── Quotes ────────────────────────────────────────────────────────────────

    public async Task<StockQuoteResponse?> GetQuoteAsync(string symbol, CancellationToken ct = default)
    {
        var cacheKey = $"quote:{symbol}";
        var cached = await _cache.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            _logger.LogInformation("Cache Hit | Quote: {Symbol}", symbol);
            return JsonSerializer.Deserialize<StockQuoteResponse>((string)cached!, JsonOptions.Default);
        }

        _logger.LogInformation("Cache Miss | Fetching Quote: {Symbol}", symbol);

        // Discovery: Ensure we have the listing first
        var listing = await EnsureListingAsync(symbol, ct);

        var q = await finnhub.GetQuoteAsync(symbol, ct);
        if (q?.CurrentPrice is null or 0)
        {
            _logger.LogWarning("Quote Not Found | Symbol: {Symbol}", symbol);
            if (listing != null)
            {
                return new StockQuoteResponse(symbol, 0, 0, 0, 0, 0, 0, 0, DateTime.UtcNow);
            }
            return null;
        }

        var res = new StockQuoteResponse(
            symbol, q.CurrentPrice.Value, q.Change ?? 0, (double)(q.PercentChange ?? 0),
            q.HighPrice ?? 0, q.LowPrice ?? 0, q.OpenPrice ?? 0,
            q.PreviousClose ?? 0, DateTime.UtcNow);

        await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(res, JsonOptions.Default), TimeSpan.FromSeconds(30));
        return res;
    }

    // ── Profile / Metadata ────────────────────────────────────────────────────

    public async Task<StockProfileResponse?> GetProfileAsync(string symbol, CancellationToken ct = default)
    {
        var listing = await EnsureListingAsync(symbol, ct);
        if (listing == null) return null;

        return new StockProfileResponse(
            listing.TickerSymbol, listing.Name, listing.Exchange, listing.Currency,
            listing.Country, listing.Industry, null, null, listing.WebUrl, listing.Logo);
    }

    // ── Intelligence: Financial Metrics ───────────────────────────────────────

    public async Task<StockMetricResponse?> GetFinancialsAsync(string symbol, CancellationToken ct = default)
    {
        var cacheKey = $"metrics:{symbol}";
        var cached = await _cache.StringGetAsync(cacheKey);
        if (cached.HasValue)
            return JsonSerializer.Deserialize<StockMetricResponse>((string)cached!, JsonOptions.Default);

        var listing = await EnsureListingAsync(symbol, ct);
        
        var metric = await unitOfWork.ExecuteSynchronizedAsync(
            () => unitOfWork.Metrics.GetBySymbolAsync(symbol, ct), ct);
        
        if (metric == null)
        {
            if (listing != null)
            {
                return new StockMetricResponse(symbol, null, null, null, null, null, null, null, null, DateTime.UtcNow);
            }
            return null;
        }

        var res = new StockMetricResponse(
            symbol, metric.PeRatio, metric.PbRatio, metric.EpsBasicTtm,
            metric.DividendYield, metric.Week52High, metric.Week52Low,
            metric.RevenueGrowthTtm, metric.MarginNet, metric.LastSyncedAt);

        await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(res, JsonOptions.Default), TimeSpan.FromHours(1));
        return res;
    }

    // ── Intelligence: Earnings ────────────────────────────────────────────────

    public async Task<IEnumerable<EarningsSurpriseResponse>> GetEarningsAsync(string symbol, CancellationToken ct = default)
    {
        var items = await unitOfWork.ExecuteSynchronizedAsync(
            () => unitOfWork.Earnings.GetBySymbolAsync(symbol, ct), ct);
            
        return items.Select(e => new EarningsSurpriseResponse(
            e.Period, e.ActualEps, e.EstimateEps, e.SurprisePercent, e.ReportDate));
    }

    // ── Intelligence: Recommendations ─────────────────────────────────────────

    public async Task<IEnumerable<RecommendationResponse>> GetRecommendationsAsync(string symbol, CancellationToken ct = default)
    {
        var items = await unitOfWork.ExecuteSynchronizedAsync(
            () => unitOfWork.Recommendations.GetBySymbolAsync(symbol, ct), ct);
            
        return items.Select(r => new RecommendationResponse(
            r.Period, r.StrongBuy, r.Buy, r.Hold, r.Sell, r.StrongSell));
    }

    // ── Intelligence: Insiders ────────────────────────────────────────────────

    public async Task<IEnumerable<InsiderTransactionResponse>> GetInsidersAsync(string symbol, CancellationToken ct = default)
    {
        var items = await unitOfWork.ExecuteSynchronizedAsync(
            () => unitOfWork.Insiders.GetBySymbolAsync(symbol, ct), ct);
            
        return items.Select(i => new InsiderTransactionResponse(
            i.Name, i.Share, i.Value, i.TransactionDate, i.FilingDate, i.TransactionCode));
    }

    // ── Intelligence: Peers ───────────────────────────────────────────────────

    public async Task<PeersResponse?> GetPeersAsync(string symbol, CancellationToken ct = default)
    {
        var cacheKey = CacheKeys.Peers(symbol);
        var cached = await _cache.StringGetAsync(cacheKey);
        if (cached.HasValue)
            return JsonSerializer.Deserialize<PeersResponse>((string)cached!, JsonOptions.Default);

        var peers = await finnhub.GetPeersAsync(symbol, ct);
        var res = new PeersResponse(symbol, peers);

        await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(res, JsonOptions.Default), TimeSpan.FromDays(1));
        return res;
    }

    // ── News ──────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<NewsResponse>> GetCompanyNewsAsync(string symbol, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var entries = await companyNewsRepo.GetLatestBySymbolAsync(symbol.ToUpperInvariant(), page * pageSize, ct);
        return entries
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new NewsResponse(
                e.NewsId, e.Headline, e.Summary, e.Source, e.Url,
                DateTimeOffset.FromUnixTimeSeconds(e.Timestamp).UtcDateTime,
                e.ImageUrl, "company"));
    }

    public async Task<IEnumerable<NewsResponse>> GetMarketNewsAsync(string category, int page, int pageSize = 20, CancellationToken ct = default)
    {
        var entries = await marketNewsRepo.QueryAsync($"CATEGORY#{category.ToUpperInvariant()}", ct);
        return entries
            .OrderByDescending(x => x.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new NewsResponse(
                e.NewsId, e.Headline, e.Summary, e.Source, e.Url,
                DateTime.TryParse(e.PublishedAt, out var dt) ? dt : DateTime.UtcNow,
                e.ImageUrl, e.Category));
    }

    // ── Calendar & Status ─────────────────────────────────────────────────────

    public async Task<IEnumerable<MarketStatusResponse>> GetMarketStatusAsync(CancellationToken ct = default)
    {
        string[] exchanges = ["US", "LSE", "HKEX"];
        var results = new List<MarketStatusResponse>();

        foreach (var ex in exchanges)
        {
            var status = await finnhub.GetMarketStatusAsync(ex, ct);
            if (status != null)
            {
                results.Add(new MarketStatusResponse(ex, status.IsOpen, status.Session, status.Holiday, status.Timezone));
            }
        }

        return results;
    }

    public async Task<IEnumerable<MarketHolidayResponse>> GetMarketHolidaysAsync(string exchange, CancellationToken ct = default)
    {
        var items = await finnhub.GetMarketHolidaysAsync(exchange, ct);
        return items.Select(h => new MarketHolidayResponse(
            exchange, h.EventName ?? "Holiday",
            DateOnly.TryParse(h.AtDate, out var d) ? d : DateOnly.MinValue,
            h.TradingHour));
    }

    public async Task<IEnumerable<EarningsCalendarResponse>> GetEarningsCalendarAsync(DateOnly from, DateOnly to, string? symbol = null, CancellationToken ct = default)
    {
        var raw = await finnhub.GetEarningsCalendarAsync(from.ToString("yyyy-MM-dd"), to.ToString("yyyy-MM-dd"), ct);
        var items = raw?.Earnings ?? [];

        if (!string.IsNullOrEmpty(symbol))
            items = items.Where(x => x.Symbol == symbol).ToList();

        return items.Select(i => new EarningsCalendarResponse(
            i.Symbol ?? "", DateOnly.TryParse(i.Date, out var d) ? d : DateOnly.MinValue,
            i.EpsEstimate, i.EpsActual, i.RevenueEstimate, i.RevenueActual));
    }

    public async Task<IEnumerable<IpoCalendarResponse>> GetIpoCalendarAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var raw = await finnhub.GetIpoCalendarAsync(from.ToString("yyyy-MM-dd"), to.ToString("yyyy-MM-dd"), ct);
        return (raw?.Items ?? []).Select(i => new IpoCalendarResponse(
            i.Symbol ?? "", i.Name ?? "Unknown",
            DateOnly.TryParse(i.Date, out var d) ? d : DateOnly.MinValue,
            decimal.TryParse(i.Price, out var p) ? p : null,
            i.Shares, i.Status ?? "Expected"));
    }

    // ── Search ────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<SymbolSearchResponse>> SearchSymbolsAsync(string query, CancellationToken ct = default)
    {
        var cacheKey = $"search:{query}";
        var cached = await _cache.StringGetAsync(cacheKey);
        if (cached.HasValue)
            return JsonSerializer.Deserialize<IEnumerable<SymbolSearchResponse>>((string)cached!, JsonOptions.Default) ?? [];

        var raw = await finnhub.SearchSymbolsAsync(query, ct);
        var res = (raw?.Result ?? []).Select(s => new SymbolSearchResponse(s.Symbol, s.Description, s.Type, ""));

        await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(res, JsonOptions.Default), TimeSpan.FromHours(4));
        return res;
    }

    // ── Private Discovery Helper ──────────────────────────────────────────────

    private async Task<StockListing?> EnsureListingAsync(string symbol, CancellationToken ct)
    {
        var normalized = symbol.ToUpperInvariant();

        // 1. Initial check (Synchronized)
        var listing = await unitOfWork.ExecuteSynchronizedAsync(
            () => unitOfWork.StockListings.FindBySymbolAsync(normalized, ct), ct);
        if (listing != null) return listing;

        _logger.LogInformation("Discovery Triggered | Symbol: {Symbol} not found in DB. Fetching from API.", normalized);

        // 2. Parallel API call
        var profile = await finnhub.GetProfileAsync(normalized, ct);
        if (profile == null)
        {
            _logger.LogWarning("Discovery Failed | Symbol: {Symbol} not found in external API.", normalized);
            return null;
        }

        // 3. Save (Synchronized with double-check)
        return await unitOfWork.ExecuteSynchronizedAsync(async () =>
        {
            var existing = await unitOfWork.StockListings.FindBySymbolAsync(normalized, ct);
            if (existing != null) return existing;

            _logger.LogInformation("Discovery Success | Saving new StockListing: {Symbol} ({Name})", normalized, profile.Name);

            var newListing = new StockListing
            {
                TickerSymbol = normalized.Length > 10 ? normalized[..10] : normalized,
                Name = profile.Name?.Length > 200 ? profile.Name[..200] : (profile.Name ?? normalized),
                Exchange = profile.Exchange?.Length > 50 ? profile.Exchange[..50] : profile.Exchange,
                Currency = profile.Currency?.Length > 10 ? profile.Currency[..10] : profile.Currency,
                Country = profile.Country?.Length > 10 ? profile.Country[..10] : profile.Country,
                Industry = profile.Industry?.Length > 100 ? profile.Industry[..100] : profile.Industry,
                Logo = profile.Logo?.Length > 1000 ? profile.Logo[..1000] : profile.Logo,
                WebUrl = profile.WebUrl?.Length > 1000 ? profile.WebUrl[..1000] : profile.WebUrl
            };

            await unitOfWork.StockListings.AddAsync(newListing, ct);
            await unitOfWork.SaveChangesAsync(ct);
            return newListing;
        }, ct);
    }
}
