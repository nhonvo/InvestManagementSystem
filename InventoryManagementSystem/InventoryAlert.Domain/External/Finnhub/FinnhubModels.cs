using System.Text.Json.Serialization;
namespace InventoryAlert.Domain.External.Finnhub;

public class FinnhubQuoteResponse
{
    [JsonPropertyName("c")]
    public decimal? CurrentPrice { get; set; }
    [JsonPropertyName("d")]
    public decimal? Change { get; set; }
    [JsonPropertyName("dp")]
    public decimal? PercentChange { get; set; }
    [JsonPropertyName("h")]
    public decimal? HighPrice { get; set; }
    [JsonPropertyName("l")]
    public decimal? LowPrice { get; set; }
    [JsonPropertyName("o")]
    public decimal? OpenPrice { get; set; }
    [JsonPropertyName("pc")]
    public decimal? PreviousClose { get; set; }
    [JsonPropertyName("t")]
    public long? Timestamp { get; set; }
}

public sealed class FinnhubNewsItem
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
    [JsonPropertyName("category")]
    public string? Category { get; set; }
    [JsonPropertyName("id")]
    public long Id { get; set; }
    [JsonPropertyName("datetime")]
    public long Datetime { get; set; }
}

public sealed class FinnhubRecommendation
{
    [JsonPropertyName("period")]
    public string? Period { get; set; }
    [JsonPropertyName("strongBuy")]
    public int StrongBuy { get; set; }
    [JsonPropertyName("buy")]
    public int Buy { get; set; }
    [JsonPropertyName("hold")]
    public int Hold { get; set; }
    [JsonPropertyName("sell")]
    public int Sell { get; set; }
    [JsonPropertyName("strongSell")]
    public int StrongSell { get; set; }
}

public sealed class FinnhubEarnings
{
    [JsonPropertyName("period")]
    public string? Period { get; set; }
    [JsonPropertyName("actual")]
    public double? Actual { get; set; }
    [JsonPropertyName("estimate")]
    public double? Estimate { get; set; }
    [JsonPropertyName("surprisePercent")]
    public double? SurprisePercent { get; set; }
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }
    [JsonPropertyName("reportDate")]
    public string? ReportDate { get; set; }
}

public sealed class FinnhubMarketStatus
{
    [JsonPropertyName("exchange")]
    public string? Exchange { get; set; }
    [JsonPropertyName("isOpen")]
    public bool IsOpen { get; set; }
    [JsonPropertyName("session")]
    public string? Session { get; set; }
    [JsonPropertyName("holiday")]
    public string? Holiday { get; set; }
    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }
}

public sealed class FinnhubHoliday
{
    [JsonPropertyName("atDate")]
    public string? AtDate { get; set; }
    [JsonPropertyName("eventName")]
    public string? EventName { get; set; }
    [JsonPropertyName("tradingHour")]
    public string? TradingHour { get; set; }
}

public sealed class FinnhubEarningsCalendar
{
    [JsonPropertyName("earningsCalendar")]
    public List<FinnhubEarningsItem> Earnings { get; set; } = [];
}

public sealed class FinnhubEarningsItem
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }
    [JsonPropertyName("date")]
    public string? Date { get; set; }
    [JsonPropertyName("epsEstimate")]
    public decimal? EpsEstimate { get; set; }
    [JsonPropertyName("epsActual")]
    public decimal? EpsActual { get; set; }
    [JsonPropertyName("revenueEstimate")]
    public decimal? RevenueEstimate { get; set; }
    [JsonPropertyName("revenueActual")]
    public decimal? RevenueActual { get; set; }
}

public sealed class FinnhubSymbolSearch
{
    [JsonPropertyName("count")]
    public int Count { get; set; }
    [JsonPropertyName("result")]
    public List<FinnhubSymbolResult> Result { get; set; } = [];
}

public sealed class FinnhubSymbolResult
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    [JsonPropertyName("displaySymbol")]
    public string DisplaySymbol { get; set; } = string.Empty;
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

public sealed class FinnhubProfileResponse
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("logo")]
    public string? Logo { get; set; }
    [JsonPropertyName("finnhubIndustry")]
    public string? Industry { get; set; }
    [JsonPropertyName("exchange")]
    public string? Exchange { get; set; }
    [JsonPropertyName("marketCapitalization")]
    public decimal? MarketCap { get; set; }
    [JsonPropertyName("ipo")]
    public string? IpoDate { get; set; }
    [JsonPropertyName("weburl")]
    public string? WebUrl { get; set; }
    [JsonPropertyName("country")]
    public string? Country { get; set; }
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }
}

public sealed class FinnhubMetricsResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }
    [JsonPropertyName("metric")]
    public Dictionary<string, double>? Metric { get; set; }
    // Note: Finnhub returns metrics as a flat dictionary for some values and nested for others.
    // For Basic Financials (/stock/metric), it's usually in the "metric" field.
}

public sealed class FinnhubInsiderResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }
    [JsonPropertyName("data")]
    public List<FinnhubInsiderItem> Data { get; set; } = [];
}

public sealed class FinnhubInsiderItem
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("share")]
    public long? Share { get; set; }
    [JsonPropertyName("change")]
    public long? Change { get; set; }
    [JsonPropertyName("filingDate")]
    public string? FilingDate { get; set; }
    [JsonPropertyName("transactionDate")]
    public string? TransactionDate { get; set; }
    [JsonPropertyName("transactionCode")]
    public string? TransactionCode { get; set; }
    [JsonPropertyName("transactionPrice")]
    public decimal? TransactionPrice { get; set; }
}

public sealed class FinnhubIpoCalendar
{
    [JsonPropertyName("ipoCalendar")]
    public List<FinnhubIpoItem> Items { get; set; } = [];
}

public sealed class FinnhubIpoItem
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("date")]
    public string? Date { get; set; }
    [JsonPropertyName("price")]
    public string? Price { get; set; } // Price can be a range like "10-12"
    [JsonPropertyName("numberOfShares")]
    public long? Shares { get; set; }
    [JsonPropertyName("totalValue")]
    public decimal? TotalValue { get; set; }
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
