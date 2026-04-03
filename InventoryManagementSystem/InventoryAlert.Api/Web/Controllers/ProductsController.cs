using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAlert.Api.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController(IProductService productService) : ControllerBase
    {
        private readonly IProductService _productService = productService;

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ProductResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProducts(CancellationToken cancellationToken)
        {
            var result = await _productService.GetAllProductsAsync(cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProductById([FromRoute] int id, CancellationToken cancellationToken)
        {
            var result = await _productService.GetProductByIdAsync(id, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateProduct([FromBody] ProductRequest request, CancellationToken cancellationToken)
        {
            var result = await _productService.CreateProductAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetProductById), new { id = result.Id }, result);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProduct([FromRoute] int id, [FromBody] ProductRequest request, CancellationToken cancellationToken)
        {
            var result = await _productService.UpdateProductAsync(id, request, cancellationToken);
            return Ok(result);
        }

        [HttpPatch("{id:int}/stock")]
        [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStockCount([FromRoute] int id, [FromQuery] int stockCount, CancellationToken cancellationToken)
        {
            var product = await _productService.GetProductByIdAsync(id, cancellationToken);
            if (product is null) return NotFound();

            var request = new ProductRequest
            {
                Name = product.Name,
                TickerSymbol = product.TickerSymbol,
                OriginPrice = product.OriginPrice,
                CurrentPrice = product.CurrentPrice,
                PriceAlertThreshold = product.PriceAlertThreshold,
                StockAlertThreshold = product.StockAlertThreshold,
                StockCount = stockCount,
            };

            var result = await _productService.UpdateProductAsync(id, request, cancellationToken);
            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteProduct([FromRoute] int id, CancellationToken cancellationToken)
        {
            var result = await _productService.DeleteProductAsync(id, cancellationToken);
            return result is null ? NotFound() : NoContent();
        }

        [HttpPost("bulk")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> BulkInsertProducts([FromBody] IEnumerable<ProductRequest> requests, CancellationToken cancellationToken)
        {
            await _productService.BulkInsertProductsAsync(requests, cancellationToken);
            return NoContent();
        }

        [HttpGet("price-alerts")]
        [ProducesResponseType(typeof(IEnumerable<PriceLossResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPriceLossAlerts(CancellationToken cancellationToken)
        {
            var result = await _productService.GetPriceLossAlertsAsync(cancellationToken);
            return Ok(result);
        }
    }
}
