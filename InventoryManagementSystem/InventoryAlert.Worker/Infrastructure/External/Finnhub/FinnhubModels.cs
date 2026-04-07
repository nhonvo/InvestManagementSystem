using System.Text.Json.Serialization;

namespace InventoryAlert.Worker.Infrastructure.External.Finnhub;

public sealed class NewsArticle
{
    [JsonPropertyName("headline")] 
    public string? Headline { get; set; }
    
    [JsonPropertyName("summary")] 
    public string? Summary { get; set; }
    
    [JsonPropertyName("source")] 
    public string? Source { get; set; }
    
    [JsonPropertyName("url")] 
    public string? Url { get; set; }
    
    [JsonPropertyName("image")] 
    public string? Image { get; set; }
    
    [JsonPropertyName("id")] 
    public long Id { get; set; }
    
    [JsonPropertyName("datetime")] 
    public long Datetime { get; set; }
}

public sealed class FinnhubQuoteModel
{
    [JsonPropertyName("c")] 
    public decimal? CurrentPrice { get; set; }

    [JsonPropertyName("dp")]
    public decimal? PercentChange { get; set; }
}
