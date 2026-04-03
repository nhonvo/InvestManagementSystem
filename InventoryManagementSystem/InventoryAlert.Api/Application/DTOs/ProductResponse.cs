namespace InventoryAlert.Api.Application.DTOs
{
    /// <summary>Used for outgoing API responses. Includes Id.</summary>
    public class ProductResponse : ProductRequest
    {
        public int Id { get; set; }
    }
}
