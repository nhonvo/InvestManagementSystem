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
    private readonly IFinnhubClient _finnhub = finnhub;
    private readonly IDatabase _cache = redis.GetDatabase();
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMarketNewsDynamoRepository _marketNewsRepo = marketNewsRepo;
    private readonly ICompanyNewsDynamoRepository _companyNewsRepo = companyNewsRepo;
    private readonly ILogger<StockDataService> _logger = logger;
    private static readonly JsonSerializerOptions _json = JsonOptions.Default;

    // ── Quotes ────────────────────────────────────────────────────────────────

    public async Task<StockQuoteResponse?> GetQuoteAsync(string symbol, CancellationToken ct = default)
    {
        var cacheKey = $"quote:{symbol}";
        var cached = await _cache.StringGetAsync(cacheKey);
        if (cached.HasValue)
            return JsonSerializer.Deserialize<StockQuoteResponse>((string)cached!, _json);

        // Discovery: Ensure we have the listing first
        var listing = await EnsureListingAsync(symbol, ct);

        var q = await _finnhub.GetQuoteAsync(symbol, ct);
        if (q?.CurrentPrice is null or 0)
        {
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

        await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(res, _json), TimeSpan.FromSeconds(30));
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
            return JsonSerializer.Deserialize<StockMetricResponse>((string)cached!, _json);

        var listing = await EnsureListingAsync(symbol, ct);
        
        var metric = await _unitOfWork.ExecuteSynchronizedAsync(
            () => _unitOfWork.Metrics.GetBySymbolAsync(symbol, ct), ct);
        
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

        await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(res, _json), TimeSpan.FromHours(1));
        return res;
    }

    // ── Intelligence: Earnings ────────────────────────────────────────────────

    public async Task<IEnumerable<EarningsSurpriseResponse>> GetEarningsAsync(string symbol, CancellationToken ct = default)
    {
        var items = await _unitOfWork.ExecuteSynchronizedAsync(
            () => _unitOfWork.Earnings.GetBySymbolAsync(symbol, ct), ct);
            
        return items.Select(e => new EarningsSurpriseResponse(
            e.Period, e.ActualEps, e.EstimateEps, e.SurprisePercent, e.ReportDate));
    }

    // ── Intelligence: Recommendations ─────────────────────────────────────────

    public async Task<IEnumerable<RecommendationResponse>> GetRecommendationsAsync(string symbol, CancellationToken ct = default)
    {
        var items = await _unitOfWork.ExecuteSynchronizedAsync(
            () => _unitOfWork.Recommendations.GetBySymbolAsync(symbol, ct), ct);
            
        return items.Select(r => new RecommendationResponse(
            r.Period, r.StrongBuy, r.Buy, r.Hold, r.Sell, r.StrongSell));
    }

    // ── Intelligence: Insiders ────────────────────────────────────────────────

    public async Task<IEnumerable<InsiderTransactionResponse>> GetInsidersAsync(string symbol, CancellationToken ct = default)
    {
        var items = await _unitOfWork.ExecuteSynchronizedAsync(
            () => _unitOfWork.Insiders.GetBySymbolAsync(symbol, ct), ct);
            
        return items.Select(i => new InsiderTransactionResponse(
            i.Name, i.Share, i.Value, i.TransactionDate, i.FilingDate, i.TransactionCode));
    }

    // ── Intelligence: Peers ───────────────────────────────────────────────────

    public async Task<PeersResponse?> GetPeersAsync(string symbol, CancellationToken ct = default)
    {
        var cacheKey = CacheKeys.Peers(symbol);
        var cached = await _cache.StringGetAsync(cacheKey);
        if (cached.HasValue)
            return JsonSerializer.Deserialize<PeersResponse>((string)cached!, _json);

        var peers = await _finnhub.GetPeersAsync(symbol, ct);
        var res = new PeersResponse(symbol, peers);

        await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(res, _json), TimeSpan.FromDays(1));
        return res;
    }

    // ── News ──────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<NewsResponse>> GetCompanyNewsAsync(string symbol, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var entries = await _companyNewsRepo.GetLatestBySymbolAsync(symbol.ToUpperInvariant(), page * pageSize, ct);
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
        var entries = await _marketNewsRepo.QueryAsync($"CATEGORY#{category.ToUpperInvariant()}", ct);
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
            var status = await _finnhub.GetMarketStatusAsync(ex, ct);
            if (status != null)
            {
                results.Add(new MarketStatusResponse(ex, status.IsOpen, status.Session, status.Holiday, status.Timezone));
            }
        }

        return results;
    }

    public async Task<IEnumerable<MarketHolidayResponse>> GetMarketHolidaysAsync(string exchange, CancellationToken ct = default)
    {
        var items = await _finnhub.GetMarketHolidaysAsync(exchange, ct);
        return items.Select(h => new MarketHolidayResponse(
            exchange, h.EventName ?? "Holiday",
            DateOnly.TryParse(h.AtDate, out var d) ? d : DateOnly.MinValue,
            h.TradingHour));
    }

    public async Task<IEnumerable<EarningsCalendarResponse>> GetEarningsCalendarAsync(DateOnly from, DateOnly to, string? symbol = null, CancellationToken ct = default)
    {
        var raw = await _finnhub.GetEarningsCalendarAsync(from.ToString("yyyy-MM-dd"), to.ToString("yyyy-MM-dd"), ct);
        var items = raw?.Earnings ?? [];

        if (!string.IsNullOrEmpty(symbol))
            items = items.Where(x => x.Symbol == symbol).ToList();

        return items.Select(i => new EarningsCalendarResponse(
            i.Symbol ?? "", DateOnly.TryParse(i.Date, out var d) ? d : DateOnly.MinValue,
            i.EpsEstimate, i.EpsActual, i.RevenueEstimate, i.RevenueActual));
    }

    public async Task<IEnumerable<IpoCalendarResponse>> GetIpoCalendarAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var raw = await _finnhub.GetIpoCalendarAsync(from.ToString("yyyy-MM-dd"), to.ToString("yyyy-MM-dd"), ct);
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
            return JsonSerializer.Deserialize<IEnumerable<SymbolSearchResponse>>((string)cached!, _json) ?? [];

        var raw = await _finnhub.SearchSymbolsAsync(query, ct);
        var res = (raw?.Result ?? []).Select(s => new SymbolSearchResponse(s.Symbol, s.Description, s.Type, ""));

        await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(res, _json), TimeSpan.FromHours(4));
        return res;
    }

    // ── Private Discovery Helper ──────────────────────────────────────────────

    private async Task<StockListing?> EnsureListingAsync(string symbol, CancellationToken ct)
    {
        var normalized = symbol.ToUpperInvariant();

        // 1. Initial check (Synchronized)
        var listing = await _unitOfWork.ExecuteSynchronizedAsync(
            () => _unitOfWork.StockListings.FindBySymbolAsync(normalized, ct), ct);
        if (listing != null) return listing;

        // 2. Parallel API call
        var profile = await _finnhub.GetProfileAsync(normalized, ct);
        if (profile == null) return null;

        // 3. Save (Synchronized with double-check)
        return await _unitOfWork.ExecuteSynchronizedAsync(async () =>
        {
            var existing = await _unitOfWork.StockListings.FindBySymbolAsync(normalized, ct);
            if (existing != null) return existing;

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

            await _unitOfWork.StockListings.AddAsync(newListing, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return newListing;
        }, ct);
    }
}
