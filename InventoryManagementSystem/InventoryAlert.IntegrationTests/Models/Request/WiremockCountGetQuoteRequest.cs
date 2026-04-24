namespace InventoryAlert.IntegrationTests.Models.Request;

public class WiremockCountGetQuoteRequest
{
    public string Method { get; set; }
    public string UrlPath { get; set; }
    public Dictionary<string, MatchPattern> Headers { get; set; }
    public Dictionary<string, MatchPattern> QueryParameters { get; set; }
}

public class MatchPattern
{
    public string Matches { get; set; }
}
