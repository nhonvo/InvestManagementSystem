using System.ComponentModel;

namespace InventoryAlert.Api.Application.DTOs;

/// <summary>
/// Parameters for filtering, sorting, and paginating products.
/// </summary>
public class ProductQueryParams
{
    private const int MaxPageSize = 50;

    private int _pageNumber = 1;
    /// <summary>
    /// The page number to retrieve.
    /// </summary>
    /// <example>1</example>
    [DefaultValue(1)]
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    private int _pageSize = 10;
    /// <summary>
    /// Number of items per page (Max 50).
    /// </summary>
    /// <example>10</example>
    [DefaultValue(10)]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 1 : (value > MaxPageSize ? MaxPageSize : value);
    }

    /// <summary>
    /// Filter by product name (partial match, case-insensitive).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Filter by minimum stock count.
    /// </summary>
    public int? MinStock { get; set; }

    /// <summary>
    /// Filter by maximum stock count.
    /// </summary>
    public int? MaxStock { get; set; }

    /// <summary>
    /// Sorting criteria. Supported: name_asc, name_desc, price_asc, price_desc, stock_asc, stock_desc.
    /// </summary>
    /// <example>name_asc</example>
    [DefaultValue("name_asc")]
    public string? SortBy { get; set; } = "name_asc";
}
