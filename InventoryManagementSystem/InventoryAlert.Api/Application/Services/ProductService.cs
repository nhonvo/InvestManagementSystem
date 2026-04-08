using FluentValidation;
using InventoryAlert.Api.Application.Common.Exceptions;
using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Application.Mappings;
using InventoryAlert.Api.Domain.Constants;
using InventoryAlert.Api.Domain.Interfaces;
using InventoryAlert.Api.Web.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace InventoryAlert.Api.Application.Services;

public class ProductService(
    IUnitOfWork unitOfWork,
    IProductRepository productRepository,
    IFinnhubClient finnhubClient,
    IMemoryCache cache,
    AppSettings appSettings,
    IValidator<ProductRequest> validator,
    ILogger<ProductService> logger) : IProductService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IFinnhubClient _finnhubClient = finnhubClient;
    private readonly IMemoryCache _cache = cache;
    private readonly AppSettings _appSettings = appSettings;
    private readonly IValidator<ProductRequest> _validator = validator;
    private readonly ILogger<ProductService> _logger = logger;

    public async Task<PagedResult<ProductResponse>> GetProductsPagedAsync(ProductQueryParams queryParams, CancellationToken cancellationToken)
    {
        // Defensive check: ensure pagination is valid before reaching repository
        if (queryParams.PageNumber < 1) queryParams.PageNumber = 1;
        if (queryParams.PageSize < 1) queryParams.PageSize = 10;

        var (items, totalCount) = await _productRepository.GetPagedAsync(
            queryParams.Name, queryParams.MinStock, queryParams.MaxStock, queryParams.SortBy,
            queryParams.PageNumber, queryParams.PageSize, cancellationToken);

        return new PagedResult<ProductResponse>
        {
            Items = items.ToResponse(),
            TotalItems = totalCount,
            PageNumber = queryParams.PageNumber,
            PageSize = queryParams.PageSize,
        };
    }

    public async Task<IEnumerable<ProductResponse>> GetAllProductsAsync(CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);
        return products.ToResponse();
    }

    public async Task<ProductResponse?> GetProductByIdAsync(int id, CancellationToken cancellationToken)
    {
        var cacheKey = $"Product_{id}";
        if (!_cache.TryGetValue(cacheKey, out ProductResponse? response))
        {
            var product = await _productRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new UserFriendlyException(ErrorCode.NotFound, string.Format(ApplicationConstants.Messages.ProductNotFound, id));


            response = product.ToResponse();
            var ttlMinutes = _appSettings.Cache.ProductTtlMinutes;
            _cache.Set(cacheKey, response, TimeSpan.FromMinutes(ttlMinutes));
        }
        return response;
    }

    public async Task<ProductResponse> CreateProductAsync(ProductRequest request, CancellationToken cancellationToken)
    {
        // Enforce Unique Ticker Symbol
        var otherWithTicker = await _productRepository.GetByTickerAsync(request.TickerSymbol, cancellationToken);
        if (otherWithTicker != null)
        {
            throw new UserFriendlyException(ErrorCode.Conflict, string.Format("Ticker '{0}' already exists.", request.TickerSymbol));
        }

        var product = request.ToEntity();
        var created = await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created.ToResponse();
    }

    public async Task<ProductResponse> UpdateProductAsync(int id, ProductRequest request, CancellationToken cancellationToken)
    {
        ProductResponse result = null!;
        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            var existing = await _productRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new UserFriendlyException(ErrorCode.NotFound, string.Format(ApplicationConstants.Messages.ProductNotFound, id));

            // Check if ticker is being changed to another product's ticker
            if (existing.TickerSymbol != request.TickerSymbol)
            {
                var otherWithTicker = await _productRepository.GetByTickerAsync(request.TickerSymbol, cancellationToken);
                if (otherWithTicker != null)
                {
                    throw new UserFriendlyException(ErrorCode.Conflict, string.Format("Ticker '{0}' already exists.", request.TickerSymbol));
                }
            }

            existing.Name = request.Name ?? string.Empty;
            existing.TickerSymbol = request.TickerSymbol ?? string.Empty;
            existing.StockCount = request.StockCount;
            // Note: CurrentPrice is managed by FinnhubSync and should not be reset during update.
            existing.OriginPrice = request.Price;
            existing.PriceAlertThreshold = request.PriceAlertThreshold ?? 0.1;

            var updated = await _productRepository.UpdateAsync(existing);
            result = updated.ToResponse();
        }, cancellationToken);

        _cache.Remove($"Product_{id}");

        return result;
    }

    public async Task<ProductResponse?> DeleteProductAsync(int id, CancellationToken cancellationToken)
    {
        var existing = await _productRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new UserFriendlyException(ErrorCode.NotFound, string.Format(ApplicationConstants.Messages.ProductNotFound, id));

        ProductResponse result = null!;
        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            var deleted = await _productRepository.DeleteAsync(existing);
            result = deleted.ToResponse();
        }, cancellationToken);

        _cache.Remove($"Product_{id}");

        return result;
    }

    public async Task<ProductResponse> UpdateStockCountAsync(int id, int newCount, CancellationToken cancellationToken)
    {
        ProductResponse result = null!;
        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            var existing = await _productRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new UserFriendlyException(ErrorCode.NotFound, string.Format(ApplicationConstants.Messages.ProductNotFound, id));

            existing.StockCount = newCount;
            var updated = await _productRepository.UpdateAsync(existing);
            result = updated.ToResponse();
        }, cancellationToken);

        _cache.Remove($"Product_{id}");
        return result;
    }


    public async Task BulkInsertProductsAsync(IEnumerable<ProductRequest> requests, CancellationToken cancellationToken)
    {
        if (!requests.Any()) return;

        var errors = new List<string>();
        foreach (var (req, index) in requests.Select((v, i) => (v, i)))
        {
            var vr = await _validator.ValidateAsync(req, cancellationToken);
            if (!vr.IsValid)
            {
                errors.Add($"Item {index + 1} ('{req.Name}'): " + string.Join("; ", vr.Errors.Select(e => e.ErrorMessage)));
            }
        }

        if (errors.Count != 0)
        {
            throw new UserFriendlyException(ErrorCode.BadRequest,
                "Bulk validation failed: " + string.Join(" | ", errors));
        }

        // Check for duplicates within the request
        var duplicateTickersInRequest = requests
            .GroupBy(r => r.TickerSymbol)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateTickersInRequest.Count != 0)
        {
            throw new UserFriendlyException(ErrorCode.Conflict,
                "Bulk insert contains duplicate ticker symbols within the request: " + string.Join(", ", duplicateTickersInRequest));
        }

        // Check for existing tickers in DB
        var incomingTickers = requests.Select(r => r.TickerSymbol).Distinct().ToList();
        var duplicatesInDb = await _productRepository.GetExistingTickersAsync(incomingTickers, cancellationToken);
        var existingTickers = duplicatesInDb.ToList();

        if (existingTickers.Count != 0)
        {
            throw new UserFriendlyException(ErrorCode.Conflict,
                "Bulk insert contains tickers that already exist in the database: " + string.Join(", ", existingTickers));
        }

        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            var entities = requests.ToEntity();
            await _productRepository.AddRangeAsync(entities, cancellationToken);
        }, cancellationToken);
    }

    public async Task<IEnumerable<PriceLossResponse>> GetPriceLossAlertsAsync(CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);
        var alerts = new List<PriceLossResponse>();
        var updatedProducts = new List<Product>();
        var cooldown = TimeSpan.FromHours(1);

        foreach (var product in products)
        {
            if (product?.CurrentPrice is 0 || product?.OriginPrice is 0) continue;

            // Only re-alert if LastAlertSentAt is null OR cooldown has elapsed
            if (product!.LastAlertSentAt.HasValue &&
                DateTime.UtcNow - product.LastAlertSentAt.Value < cooldown)
                continue;

            var priceDiff = product.CurrentPrice - product.OriginPrice;
            var priceChangePercent = priceDiff / product.OriginPrice;

            if (priceChangePercent < 0)
            {
                var lossMagnitude = Math.Abs(priceChangePercent);
                if (lossMagnitude >= (decimal)product.PriceAlertThreshold)
                {
                    product.LastAlertSentAt = DateTime.UtcNow;
                    updatedProducts.Add(product);

                    alerts.Add(new PriceLossResponse
                    {
                        Id = product.Id,
                        Name = product.Name,
                        TickerSymbol = product.TickerSymbol,
                        OriginPrice = product.OriginPrice,
                        CurrentPrice = product.CurrentPrice,
                        PriceDiff = Math.Abs(priceDiff),
                        PriceChangePercent = Math.Round(priceChangePercent, 4),
                        PriceAlertThreshold = product.PriceAlertThreshold,
                        StockCount = product.StockCount,
                    });
                }
            }
        }

        if (updatedProducts.Count > 0)
        {
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                await _productRepository.UpdateRangeAsync(updatedProducts);
            }, cancellationToken);
        }

        return alerts;
    }

    public async Task SyncCurrentPricesAsync(CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);
        var updated = new List<Product>();

        foreach (var product in products)
        {
            var quote = await _finnhubClient.GetQuoteAsync(product.TickerSymbol, cancellationToken);
            if (quote?.CurrentPrice is null or 0)
            {
                _logger.LogWarning("[ProductService] Null or zero price returned for {Symbol}. Skipping.", product.TickerSymbol);
                continue;
            }

            product.CurrentPrice = quote.CurrentPrice.Value;
            updated.Add(product);
        }

        if (updated.Count > 0)
        {
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                await _productRepository.UpdateRangeAsync(updated);
            }, cancellationToken);
        }
    }
}
