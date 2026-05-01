namespace InventoryAlert.IntegrationTests.Models.Request;

public class WiremockCountGetQuoteRequest
{
    public string Method { get; set; } = string.Empty;
    public string UrlPath { get; set; } = string.Empty;
    public Dictionary<string, MatchPattern> Headers { get; set; } = [];
    public Dictionary<string, MatchPattern> QueryParameters { get; set; } = [];
}

public class MatchPattern
{
    public string Matches { get; set; } = string.Empty;
}
