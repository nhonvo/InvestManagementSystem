using FluentValidation;
using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Application.Mappings;
using InventoryAlert.Api.Web.Configuration;
using InventoryAlert.Contracts.Common.Constants;
using InventoryAlert.Contracts.Common.Exceptions;
using InventoryAlert.Contracts.Entities;
using InventoryAlert.Contracts.Persistence.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace InventoryAlert.Api.Application.Services;

public class ProductService(
    IUnitOfWork unitOfWork,
    IProductRepository productRepository,
    IFinnhubClient finnhubClient,
    IMemoryCache cache,
    AppSettings appSettings,
    IValidator<ProductRequest> validator,
    IStockTransactionRepository stockTxRepo,
    ILogger<ProductService> logger) : IProductService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IFinnhubClient _finnhubClient = finnhubClient;
    private readonly IMemoryCache _cache = cache;
    private readonly AppSettings _appSettings = appSettings;
    private readonly IValidator<ProductRequest> _validator = validator;
    private readonly IStockTransactionRepository _stockTxRepo = stockTxRepo;
    private readonly ILogger<ProductService> _logger = logger;

    public async Task<PagedResult<ProductResponse>> GetProductsPagedAsync(ProductQueryParams queryParams, CancellationToken cancellationToken)
    {
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
            PageSize = queryParams.PageSize
        };
    }

    public async Task<IEnumerable<ProductResponse>> GetAllProductsAsync(CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);
        return products.ToResponse();
    }

    public async Task<ProductResponse?> GetProductByIdAsync(int id, CancellationToken cancellationToken)
    {
        var cacheKey = $"product_{id}";
        if (_cache.TryGetValue(cacheKey, out ProductResponse? cached))
        {
            return cached;
        }

        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product == null)
            throw new UserFriendlyException(ErrorCode.NotFound, string.Format(ApplicationConstants.Messages.ProductNotFound, id));

        var response = product.ToResponse();
        _cache.Set(cacheKey, response, TimeSpan.FromMinutes(_appSettings.Cache.ProductTtlMinutes));
        return response;
    }

    public async Task<ProductResponse> CreateProductAsync(ProductRequest request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var existingWithTicker = await _productRepository.GetByTickerAsync(request.TickerSymbol ?? string.Empty, cancellationToken);
        if (existingWithTicker != null)
        {
            throw new UserFriendlyException(ErrorCode.Conflict, string.Format("Ticker '{0}' already exists.", request.TickerSymbol));
        }

        var entity = request.ToEntity();
        var created = await _productRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return created.ToResponse();
    }

    public async Task<ProductResponse> UpdateProductAsync(int id, ProductRequest request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        ProductResponse result = null!;
        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            var existing = await _productRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new UserFriendlyException(ErrorCode.NotFound, string.Format(ApplicationConstants.Messages.ProductNotFound, id));

            if (existing.TickerSymbol != request.TickerSymbol)
            {
                var otherWithTicker = await _productRepository.GetByTickerAsync(request.TickerSymbol ?? string.Empty, cancellationToken);
                if (otherWithTicker != null)
                {
                    throw new UserFriendlyException(ErrorCode.Conflict, string.Format("Ticker '{0}' already exists.", request.TickerSymbol));
                }
            }

            existing.Name = request.Name ?? string.Empty;
            existing.TickerSymbol = request.TickerSymbol ?? string.Empty;
            existing.StockCount = request.StockCount;
            existing.OriginPrice = request.Price;
            existing.PriceAlertThreshold = request.PriceAlertThreshold ?? 0.1;

            var updated = await _productRepository.UpdateAsync(existing);
            result = updated.ToResponse();
        }, cancellationToken);

        _cache.Remove($"product_{id}");
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

        _cache.Remove($"product_{id}");
        return result;
    }

    public async Task<ProductResponse> UpdateStockCountAsync(int id, int newCount, string userId, CancellationToken cancellationToken)
    {
        ProductResponse result = null!;
        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            var existing = await _productRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new UserFriendlyException(ErrorCode.NotFound, string.Format(ApplicationConstants.Messages.ProductNotFound, id));

            var diff = newCount - existing.StockCount;
            if (diff == 0)
            {
                result = existing.ToResponse();
                return;
            }

            existing.StockCount = newCount;
            var updated = await _productRepository.UpdateAsync(existing);

            // Record transaction audit
            await _stockTxRepo.AddAsync(new StockTransaction
            {
                ProductId = id,
                UserId = userId,
                Quantity = diff,
                Type = diff > 0 ? StockTransactionType.Restock : StockTransactionType.Adjustment,
                Timestamp = DateTime.UtcNow,
                Reference = "Manual API Update"
            }, cancellationToken);

            result = updated.ToResponse();
        }, cancellationToken);

        _cache.Remove($"product_{id}");
        return result;
    }

    public async Task BulkInsertProductsAsync(IEnumerable<ProductRequest> requests, CancellationToken cancellationToken)
    {
        if (!requests.Any()) return;

        var tickers = requests.Select(r => r.TickerSymbol ?? string.Empty).Where(t => t != string.Empty).Distinct();
        var duplicates = await _productRepository.GetExistingTickersAsync(tickers, cancellationToken);
        if (duplicates.Any())
        {
            throw new UserFriendlyException(ErrorCode.Conflict, $"Tickers already exist: {string.Join(", ", duplicates)}");
        }

        var entities = requests.ToEntity();
        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            await _productRepository.AddRangeAsync(entities, cancellationToken);
        }, cancellationToken);
    }

    public async Task<IEnumerable<PriceLossResponse>> GetPriceLossAlertsAsync(CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);
        var alerts = new List<PriceLossResponse>();
        var updatedProducts = new List<Product>();

        foreach (var product in products)
        {
            if (product.OriginPrice > 0 && product.CurrentPrice > 0)
            {
                var priceDiff = product.CurrentPrice - product.OriginPrice;
                var priceChangePercent = priceDiff / product.OriginPrice;
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
        if (!products.Any()) return;

        var updated = new System.Collections.Concurrent.ConcurrentBag<Product>();
        var semaphore = new SemaphoreSlim(5);

        var tasks = products.Select(async product =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var quote = await _finnhubClient.GetQuoteAsync(product.TickerSymbol, cancellationToken);
                if (quote?.CurrentPrice is not null and > 0)
                {
                    product.CurrentPrice = quote.CurrentPrice.Value;
                    updated.Add(product);
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        if (!updated.IsEmpty)
        {
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                await _productRepository.UpdateRangeAsync(updated);
            }, cancellationToken);
        }
    }
}
