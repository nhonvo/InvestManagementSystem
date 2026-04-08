using Asp.Versioning;
using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAlert.Api.Web.Controllers;

/// <summary>CRUD + alert endpoints for inventory products.</summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
// [Authorize]
public class ProductsController(IProductService productService) : ControllerBase
{
    private readonly IProductService _productService = productService;

    /// <summary>Get all products.</summary>
    [HttpGet]
    [ResponseCache(Duration = 30, VaryByQueryKeys = ["*"])]
    [ProducesResponseType(typeof(PagedResult<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts([FromQuery] ProductQueryParams query, CancellationToken cancellationToken)
    {
        var result = await _productService.GetProductsPagedAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Get a single product by ID.</summary>
    [HttpGet("{id:int}")]
    [ResponseCache(Duration = 60, VaryByHeader = "Accept")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await _productService.GetProductByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>Create a new product.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct([FromBody] ProductRequest request, CancellationToken cancellationToken)
    {
        var result = await _productService.CreateProductAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetProductById), new { id = result.Id }, result);
    }

    /// <summary>Full update of a product.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct([FromRoute] int id, [FromBody] ProductRequest request, CancellationToken cancellationToken)
    {
        var result = await _productService.UpdateProductAsync(id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>Partial update of stock count.</summary>
    [HttpPatch("{id:int}/stock/{count:int}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchStockCount([FromRoute] int id, [FromRoute] int count, CancellationToken cancellationToken)
    {
        var result = await _productService.UpdateStockCountAsync(id, count, cancellationToken);
        return Ok(result);
    }

    /// <summary>Delete a product by ID.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await _productService.DeleteProductAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>Bulk insert products (returns 204 — no body to reduce bandwidth).</summary>
    [HttpPost("bulk")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> BulkInsertProducts([FromBody] IEnumerable<ProductRequest> requests, CancellationToken cancellationToken)
    {
        await _productService.BulkInsertProductsAsync(requests, cancellationToken);
        return NoContent();
    }

    /// <summary>Returns products whose price drop exceeds their configured alert threshold.</summary>
    [HttpGet("price-alerts")]
    [ProducesResponseType(typeof(IEnumerable<PriceLossResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPriceLossAlerts(CancellationToken cancellationToken)
    {
        var result = await _productService.GetPriceLossAlertsAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>Triggers a manual sync of CurrentPrice from Finnhub for all products.</summary>
    [HttpPost("sync-price")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SyncStockPrice(CancellationToken cancellationToken)
    {
        await _productService.SyncCurrentPricesAsync(cancellationToken);
        return NoContent();
    }
}
