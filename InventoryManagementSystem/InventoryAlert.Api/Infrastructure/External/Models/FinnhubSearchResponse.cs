using Newtonsoft.Json;

namespace InventoryAlert.Api.Infrastructure.External.Models
{

    /// <summary>
    /// 🛠️ Finnhub Integration Implementation Checklist
    /// 
    /// Follow these steps to complete the automated pricing and alert system based on the [Finnhub Docs](https://finnhub.io/docs/api/quote).
    /// </summary>
    public class FinnhubSearchResponse
    {
        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("result")]
        public List<SearchSymbolResult> Result { get; set; } = [];
    }

    public class SearchSymbolResult
    {
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("displaySymbol")]
        public string DisplaySymbol { get; set; } = string.Empty;

        [JsonProperty("symbol")]
        public string Symbol { get; set; } = string.Empty;

        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;
    }
}
