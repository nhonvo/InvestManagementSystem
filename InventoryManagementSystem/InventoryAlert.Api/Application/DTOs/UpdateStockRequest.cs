using System.ComponentModel.DataAnnotations;

namespace InventoryAlert.Api.Application.DTOs;

/// <summary>Request DTO for partial stock count updates.</summary>
public class UpdateStockRequest
{
    [Range(0, int.MaxValue, ErrorMessage = "Stock count cannot be negative.")]
    public int Count { get; set; }
}
