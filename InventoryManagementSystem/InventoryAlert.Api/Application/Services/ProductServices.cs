using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Domain.Entities;
using InventoryAlert.Api.Infrastructure.External.Interfaces;
using InventoryAlert.Api.Infrastructure.Persistence.Interfaces;
using System.Xml.Linq;

namespace InventoryAlert.Api.Application.Services
{
    public class ProductServices(IUnitOfWork unitOfWork, IProductRepository productRepository, IFinnhubClient finnhubClient) : IProductService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IProductRepository _productRepository = productRepository;
        private readonly IFinnhubClient _finnhubClient = finnhubClient;

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync(CancellationToken cancellationToken)
        {
            var products = await _productRepository.GetAllAsync(cancellationToken);

            List<ProductDto> productDtos = [];
            foreach (var product in products)
            {
                productDtos.Add(MapProductToProductDto(product));
            }
            return productDtos;
        }

        public async Task<ProductDto> GetProductByIdAsync(int id, CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetByIdAsync(id, cancellationToken);
            if (product == null) return null;
            return MapProductToProductDto(product);
        }
        public async Task<ProductDto> CreateProductAsync(ProductRequestDto productRequestDto, CancellationToken cancellationToken)
        {
            var product = MapProductRequestDtotoProduct(productRequestDto);
            var createdProduct = await _productRepository.AddAsync(product, cancellationToken);
            return MapProductToProductDto(createdProduct);
        }
        // TODO: review the input parameters for this method, should we use the id from the route or the id from the body?
        public async Task<ProductDto> UpdateProductAsync(int id, ProductRequestDto productRequestDto, CancellationToken cancellationToken)
        {
            var existingProduct = await _productRepository.GetByIdAsync(id, cancellationToken) ?? throw new Exception("Not found the product");
            existingProduct.Name = productRequestDto.Name;
            existingProduct.TickerSymbol = productRequestDto.TickerSymbol;
            existingProduct.StockCount = productRequestDto.StockCount;
            existingProduct.CurrentPrice = productRequestDto.CurrentPrice;
            existingProduct.OriginPrice = productRequestDto.OriginPrice;
            existingProduct.PriceAlertThreshold = productRequestDto.PriceAlertThreshold;
            Product updatedProduct = new();
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                updatedProduct = await _productRepository.UpdateAsync(existingProduct);
            }, CancellationToken.None);
            return MapProductToProductDto(updatedProduct);
        }
        public async Task<ProductDto> DeleteProductAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id, CancellationToken.None);
            if (product == null) return null;
            Product deletedProduct = new();
            // TODO: REVIEW FLOW USE UNIT OF WORK WITH NORMAL REPOSITORY I JUST WANT REACH ROLL BACK WHEN SCALE UP HAS MORE TABLE IF FAIL WHEN INSERT ONE TABLE ROLLBACK WHOLE THING  SO REVIEW IT SHOULD WE KEEP UNITOFWORK OF HAS ANOTHER IMPLEMENTATION WHICH SIMPLE
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                deletedProduct = await _productRepository.DeleteAsync(product);
            }, CancellationToken.None);
            return MapProductToProductDto(deletedProduct);
        }


        // TODO: suggest data response for this method, should we return the list of created products or just return the count of created products? review it
        public async Task BulkInsertProductsAsync(IEnumerable<ProductRequestDto> productRequestDtos, CancellationToken cancellationToken)
        {

            List<Product> products = [];
            foreach (var productRequestDto in productRequestDtos)
            {
                products.Add(MapProductRequestDtotoProduct(productRequestDto));
            }
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                await _productRepository.AddRangeAsync(products, cancellationToken);
            }, cancellationToken);
        }

        public async Task<IEnumerable<ProductLossDto>> GetHighValueProducts(CancellationToken cancellationToken)
        {
            var products = await _productRepository.GetAllAsync(cancellationToken);
            // TODO: review the design of this method, should we get the quote for each product in the database and compare the stock count with the alert threshold? or should we have a separate table to store the stock count and alert threshold for each product and update it regularly?
            List<ProductLossDto> productDtos = new List<ProductLossDto>();
            foreach (var product in products)
            {
                var price = await _finnhubClient.GetQuoteAsync(product.TickerSymbol, cancellationToken);
                if (price.CurrentPrice == 0 || price.CurrentPrice == null) continue;
                decimal? priceDelta = (product.OriginPrice - price.CurrentPrice) / product.OriginPrice;


                // Alert if the loss (priceDelta) is GREATER than 10%
                bool isSignificantLoss = priceDelta < (decimal)product.PriceAlertThreshold;

                if (isSignificantLoss)
                {
                    productDtos.Add(new ProductLossDto
                    {
                        Id = product.Id,
                        Name = product.Name,
                        CurrentPrice = price.CurrentPrice ?? 0, // todo: should we check if the price is null before assign it to current price? or should we set the current price to 0 if the price is null? review it
                        PriceDiff = (price.CurrentPrice ?? 0) - product.OriginPrice,
                        PriceChangePercent = Math.Round(priceDelta ?? 0, 2),
                        PriceAlertThreshold = product.PriceAlertThreshold,
                        StockCount = product.StockCount,
                        TickerSymbol = product.TickerSymbol,
                        OriginPrice = product.OriginPrice,
                    });
                }
            }
            return productDtos;
        }

        public async Task SyncCurrentPricesAsync(CancellationToken cancellationToken)
        {
            var products = await _productRepository.GetAllAsync(cancellationToken);
           
            foreach (var product in products)
            {
                var price = await _finnhubClient.GetQuoteAsync(product.TickerSymbol, cancellationToken);
                if (price.CurrentPrice == 0 || price.CurrentPrice == null) continue;
                product.CurrentPrice = price.CurrentPrice ?? 0;
            }

            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                await _productRepository.UpdateRangeAsync(products);
            }, cancellationToken);
        }

        // Should we move this method to a separate class to handle the mapping between Product and ProductDto? review it

        private static ProductDto MapProductToProductDto(Product product)
        {
            return new ProductDto()
            {
                Id = product.Id,
                Name = product.Name,
                TickerSymbol = product.TickerSymbol,
                StockCount = product.StockCount,
                PriceAlertThreshold = product.PriceAlertThreshold,
                OriginPrice = product.OriginPrice
            };
        }

        private static Product MapProductRequestDtotoProduct(ProductRequestDto productRequestDto)
        {
            return new Product()
            {
                Name = productRequestDto.Name,
                TickerSymbol = productRequestDto.TickerSymbol,
                StockCount = productRequestDto.StockCount,
                CurrentPrice = productRequestDto.CurrentPrice,
                OriginPrice = productRequestDto.OriginPrice,
                PriceAlertThreshold = productRequestDto.PriceAlertThreshold,
            };
        }
    }
}
