namespace InventoryAlert.Contracts.Entities;

public class CompanyProfile
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Logo { get; set; }
    public string? Industry { get; set; }
    public string? Exchange { get; set; }
    public decimal? MarketCap { get; set; }
    public DateOnly? IpoDate { get; set; }
    public string? WebUrl { get; set; }
    public string? Country { get; set; }
    public string? Currency { get; set; }
    public DateTime RefreshedAt { get; set; } = DateTime.UtcNow;
}
