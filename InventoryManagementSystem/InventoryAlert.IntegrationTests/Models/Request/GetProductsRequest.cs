namespace InventoryAlert.IntegrationTests.Models.Request;

public class GetProductsRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Name { get; set; }
    public string? MinStock { get; set; }
    public string? MaxStock { get; set; }
    public string? SortBy { get; set; }
}
