using InventoryAlert.Api.Application.DTOs;

namespace InventoryAlert.Api.Application.Interfaces
{
    public interface IProductService
    {
        public Task<IEnumerable<ProductDto>> GetAllProductsAsync(CancellationToken cancellationToken);
        public Task<ProductDto> GetProductByIdAsync(int id, CancellationToken cancellationToken);
        public Task<ProductDto> CreateProductAsync(ProductRequestDto productRequestDto, CancellationToken cancellationToken);
        public Task<ProductDto> UpdateProductAsync(int id, ProductRequestDto productRequestDto, CancellationToken cancellationToken);
        public Task<ProductDto> DeleteProductAsync(int id);
        
        public Task BulkInsertProductsAsync(IEnumerable<ProductRequestDto> productRequestDtos, CancellationToken cancellationToken);
        public Task<IEnumerable<ProductLossDto>> GetHighValueProducts(CancellationToken cancellationToken);
        public Task SyncCurrentPricesAsync(CancellationToken cancellationToken);
    }
}
