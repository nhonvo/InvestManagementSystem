using System.Text.Json;
using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Contracts.Common.Exceptions;
using InventoryAlert.Contracts.Entities;
using InventoryAlert.Contracts.Events;
using InventoryAlert.Contracts.Persistence.Interfaces;
using StackExchange.Redis;

namespace InventoryAlert.Api.Application.Services;

public class WatchlistService(
    IUnitOfWork unitOfWork,
    IFinnhubClient finnhub,
    IConnectionMultiplexer redis,
    IEventPublisher eventPublisher,
    ILogger<WatchlistService> logger) : IWatchlistService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IFinnhubClient _finnhub = finnhub;
    private readonly IDatabase _cache = redis.GetDatabase();
    private readonly IEventPublisher _events = eventPublisher;
    private readonly ILogger<WatchlistService> _logger = logger;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task<List<WatchlistItemResponse>> GetUserWatchlistAsync(string userId, CancellationToken ct = default)
    {
        var cacheKey = $"watchlist:{userId}";
        var cached = await _cache.StringGetAsync(cacheKey);
        if (cached.HasValue)
            return JsonSerializer.Deserialize<List<WatchlistItemResponse>>((string)cached!, _jsonOptions) ?? [];

        var items = await _unitOfWork.Watchlists.GetByUserIdAsync(userId, ct);

        var response = items.Select(w => new WatchlistItemResponse(
                w.Symbol,
                w.Product != null ? w.Product.Name : w.Symbol,
                // Product entity does not have an Exchange field yet.
                // Populate once CompanyProfile sync adds Exchange to the Product entity.
                string.Empty,
                "stock",
                w.Product != null ? (decimal?)w.Product.CurrentPrice : null,
                null, null,
                w.AddedAt))
            .ToList();

        await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(response, _jsonOptions), TimeSpan.FromMinutes(1));
        return response;
    }

    public async Task AddToWatchlistAsync(string userId, string symbol, CancellationToken ct = default)
    {
        // 1. Validate symbol existence (with Redis caching to avoid hitting Finnhub rate limits)
        var symbolKey = symbol.ToUpperInvariant();
        var validationCacheKey = $"symbol_valid:{symbolKey}";
        var isSymbolValid = await _cache.KeyExistsAsync(validationCacheKey);

        if (!isSymbolValid)
        {
            var search = await _finnhub.SearchSymbolsAsync(symbolKey, ct);
            if (search?.Result is null || !search.Result.Any(s => string.Equals(s.Symbol, symbolKey, StringComparison.OrdinalIgnoreCase)))
            {
                throw new UserFriendlyException(ErrorCode.BadRequest, $"Symbol '{symbolKey}' not found on Finnhub.");
            }
            // Cache valid symbols for 24 hours
            await _cache.StringSetAsync(validationCacheKey, "true", TimeSpan.FromHours(24));
        }

        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            var exists = await _unitOfWork.Watchlists.ExistsAsync(userId, symbol, ct);

            if (exists)
            {
                _logger.LogInformation("[WatchlistService] {UserId} already has {Symbol} in watchlist.", userId, symbol);
                return;
            }

            await _unitOfWork.Watchlists.AddAsync(new Watchlist { UserId = userId, Symbol = symbol }, ct);

            await _events.PublishAsync(new EventEnvelope
            {
                EventType = EventTypes.SymbolAdded,
                Source = "InventoryAlert.Api",
                Payload = JsonSerializer.Serialize(new { Symbol = symbol, UserId = userId })
            }, ct);

            _logger.LogInformation("[WatchlistService] Added {Symbol} to {UserId}'s watchlist.", symbol, userId);
            await _cache.KeyDeleteAsync($"watchlist:{userId}");
        }, ct);
    }

    public async Task RemoveFromWatchlistAsync(string userId, string symbol, CancellationToken ct = default)
    {
        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            var item = await _unitOfWork.Watchlists.GetAsync(userId, symbol, ct);

            if (item is null)
            {
                _logger.LogWarning("[WatchlistService] {Symbol} not found in {UserId}'s watchlist.", symbol, userId);
                return;
            }

            await _unitOfWork.Watchlists.DeleteAsync(item);

            await _events.PublishAsync(new EventEnvelope
            {
                EventType = EventTypes.SymbolRemoved,
                Source = "InventoryAlert.Api",
                Payload = JsonSerializer.Serialize(new { Symbol = symbol, UserId = userId })
            }, ct);

            _logger.LogInformation("[WatchlistService] Removed {Symbol} from {UserId}'s watchlist.", symbol, userId);
            await _cache.KeyDeleteAsync($"watchlist:{userId}");
        }, ct);
    }
}
