using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Domain.Entities;
using InventoryAlert.Api.Domain.Interfaces;
using InventoryAlert.Api.Infrastructure.External.Interfaces;

namespace InventoryAlert.Api.Application.Services
{
    public class ProductService(
        IUnitOfWork unitOfWork,
        IProductRepository productRepository,
        IFinnhubClient finnhubClient) : IProductService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IProductRepository _productRepository = productRepository;
        private readonly IFinnhubClient _finnhubClient = finnhubClient;

        public async Task<IEnumerable<ProductResponse>> GetAllProductsAsync(CancellationToken cancellationToken)
        {
            var products = await _productRepository.GetAllAsync(cancellationToken);
            return products.Select(MapToResponse);
        }

        public async Task<ProductResponse?> GetProductByIdAsync(int id, CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetByIdAsync(id, cancellationToken);
            return product is null ? null : MapToResponse(product);
        }

        public async Task<ProductResponse> CreateProductAsync(ProductRequest request, CancellationToken cancellationToken)
        {
            var product = MapToEntity(request);
            var created = await _productRepository.AddAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return MapToResponse(created);
        }

        public async Task<ProductResponse> UpdateProductAsync(int id, ProductRequest request, CancellationToken cancellationToken)
        {
            var existing = await _productRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException($"Product with id {id} was not found.");

            existing.Name = request.Name ?? string.Empty;
            existing.TickerSymbol = request.TickerSymbol ?? string.Empty;
            existing.StockCount = request.StockCount;
            existing.CurrentPrice = request.CurrentPrice;
            existing.OriginPrice = request.OriginPrice;
            existing.PriceAlertThreshold = request.PriceAlertThreshold;

            Product updated = new();
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                updated = await _productRepository.UpdateAsync(existing);
            }, cancellationToken);

            return MapToResponse(updated);
        }

        public async Task<ProductResponse?> DeleteProductAsync(int id, CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetByIdAsync(id, cancellationToken);
            if (product is null) return null;

            Product deleted = new();
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                deleted = await _productRepository.DeleteAsync(product);
            }, cancellationToken);

            return MapToResponse(deleted);
        }

        public async Task BulkInsertProductsAsync(IEnumerable<ProductRequest> requests, CancellationToken cancellationToken)
        {
            var products = requests.Select(MapToEntity).ToList();
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                await _productRepository.AddRangeAsync(products, cancellationToken);
            }, cancellationToken);
        }

        public async Task<IEnumerable<PriceLossResponse>> GetPriceLossAlertsAsync(CancellationToken cancellationToken)
        {
            var products = await _productRepository.GetAllAsync(cancellationToken);
            var alerts = new List<PriceLossResponse>();

            foreach (var product in products)
            {
                var quote = await _finnhubClient.GetQuoteAsync(product.TickerSymbol, cancellationToken);
                if (quote?.CurrentPrice is null or 0) continue;

                var priceDelta = (product.OriginPrice - quote.CurrentPrice.Value) / product.OriginPrice;
                var isSignificantLoss = priceDelta > (decimal)product.PriceAlertThreshold;

                if (isSignificantLoss)
                {
                    alerts.Add(new PriceLossResponse
                    {
                        Id = product.Id,
                        Name = product.Name,
                        TickerSymbol = product.TickerSymbol,
                        OriginPrice = product.OriginPrice,
                        CurrentPrice = quote.CurrentPrice.Value,
                        PriceDiff = quote.CurrentPrice.Value - product.OriginPrice,
                        PriceChangePercent = Math.Round(priceDelta, 2),
                        PriceAlertThreshold = product.PriceAlertThreshold,
                        StockCount = product.StockCount,
                    });
                }
            }

            return alerts;
        }

        public async Task SyncCurrentPricesAsync(CancellationToken cancellationToken)
        {
            var products = await _productRepository.GetAllAsync(cancellationToken);

            foreach (var product in products)
            {
                var quote = await _finnhubClient.GetQuoteAsync(product.TickerSymbol, cancellationToken);
                if (quote?.CurrentPrice is null or 0) continue;
                product.CurrentPrice = quote.CurrentPrice.Value;
            }

            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                await _productRepository.UpdateRangeAsync(products);
            }, cancellationToken);
        }

        // ─── Mapping Helpers ────────────────────────────────────────────────────

        private static ProductResponse MapToResponse(Product product) => new()
        {
            Id = product.Id,
            Name = product.Name,
            TickerSymbol = product.TickerSymbol,
            StockCount = product.StockCount,
            PriceAlertThreshold = product.PriceAlertThreshold,
            OriginPrice = product.OriginPrice,
            CurrentPrice = product.CurrentPrice,
        };

        private static Product MapToEntity(ProductRequest request) => new()
        {
            Name = request.Name ?? string.Empty,
            TickerSymbol = request.TickerSymbol ?? string.Empty,
            StockCount = request.StockCount,
            CurrentPrice = request.CurrentPrice,
            OriginPrice = request.OriginPrice,
            PriceAlertThreshold = request.PriceAlertThreshold,
        };
    }
}
