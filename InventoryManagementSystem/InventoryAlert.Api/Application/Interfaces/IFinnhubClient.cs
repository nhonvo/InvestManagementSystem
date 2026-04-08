using System.Text.Json.Serialization;
using InventoryAlert.Api.Application.DTOs;

namespace InventoryAlert.Api.Application.Interfaces;

public interface IFinnhubClient
{
    Task<FinnhubQuoteResponse?> GetQuoteAsync(string symbol, CancellationToken ct = default);
    Task<FinnhubProfileResponse?> GetProfileAsync(string symbol, CancellationToken ct = default);
    Task<List<FinnhubNewsItem>> GetCompanyNewsAsync(string symbol, string from, string to, CancellationToken ct = default);
    Task<List<FinnhubRecommendation>> GetRecommendationsAsync(string symbol, CancellationToken ct = default);
    Task<List<FinnhubEarnings>> GetEarningsAsync(string symbol, CancellationToken ct = default);
    Task<List<string>> GetPeersAsync(string symbol, CancellationToken ct = default);
    Task<List<FinnhubNewsItem>> GetMarketNewsAsync(string category, CancellationToken ct = default);
    Task<FinnhubMarketStatus?> GetMarketStatusAsync(string exchange, CancellationToken ct = default);
    Task<List<FinnhubHoliday>> GetMarketHolidaysAsync(string exchange, CancellationToken ct = default);
    Task<FinnhubEarningsCalendar?> GetEarningsCalendarAsync(string from, string to, CancellationToken ct = default);
    Task<FinnhubSymbolSearch?> SearchSymbolsAsync(string query, CancellationToken ct = default);
    Task<List<string>> GetCryptoExchangesAsync(CancellationToken ct = default);
    Task<List<FinnhubCryptoSymbol>> GetCryptoSymbolsAsync(string exchange, CancellationToken ct = default);
}

// ── Finnhub raw response models ──────────────────────────────────────────────

public class FinnhubProfileResponse
{
    [JsonPropertyName("name")]   public string? Name { get; set; }
    [JsonPropertyName("logo")]   public string? Logo { get; set; }
    [JsonPropertyName("finnhubIndustry")] public string? Industry { get; set; }
    [JsonPropertyName("exchange")]        public string? Exchange { get; set; }
    [JsonPropertyName("marketCapitalization")] public decimal? MarketCap { get; set; }
    [JsonPropertyName("ipo")]    public string? IpoDate { get; set; }
    [JsonPropertyName("weburl")] public string? WebUrl { get; set; }
    [JsonPropertyName("country")]   public string? Country { get; set; }
    [JsonPropertyName("currency")]  public string? Currency { get; set; }
}

public class FinnhubNewsItem
{
    [JsonPropertyName("headline")]    public string? Headline { get; set; }
    [JsonPropertyName("summary")]     public string? Summary { get; set; }
    [JsonPropertyName("source")]      public string? Source { get; set; }
    [JsonPropertyName("url")]         public string? Url { get; set; }
    [JsonPropertyName("image")]       public string? Image { get; set; }
    [JsonPropertyName("category")]    public string? Category { get; set; }
    [JsonPropertyName("datetime")]    public long Datetime { get; set; }
}

public class FinnhubRecommendation
{
    [JsonPropertyName("period")]      public string? Period { get; set; }
    [JsonPropertyName("strongBuy")]   public int StrongBuy { get; set; }
    [JsonPropertyName("buy")]         public int Buy { get; set; }
    [JsonPropertyName("hold")]        public int Hold { get; set; }
    [JsonPropertyName("sell")]        public int Sell { get; set; }
    [JsonPropertyName("strongSell")]  public int StrongSell { get; set; }
}

public class FinnhubEarnings
{
    [JsonPropertyName("period")]          public string? Period { get; set; }
    [JsonPropertyName("actual")]          public decimal? Actual { get; set; }
    [JsonPropertyName("estimate")]        public decimal? Estimate { get; set; }
    [JsonPropertyName("surprise")]        public decimal? Surprise { get; set; }
    [JsonPropertyName("surprisePercent")] public decimal? SurprisePercent { get; set; }
}

public class FinnhubMarketStatus
{
    [JsonPropertyName("exchange")]   public string? Exchange { get; set; }
    [JsonPropertyName("isOpen")]     public bool IsOpen { get; set; }
    [JsonPropertyName("session")]    public string? Session { get; set; }
    [JsonPropertyName("holiday")]    public string? Holiday { get; set; }
}

public class FinnhubHoliday
{
    [JsonPropertyName("atDate")]      public string? AtDate { get; set; }
    [JsonPropertyName("eventName")]   public string? EventName { get; set; }
    [JsonPropertyName("tradingHour")] public string? TradingHour { get; set; }
}

public class FinnhubEarningsCalendar
{
    [JsonPropertyName("earningsCalendar")]
    public List<FinnhubEarningsCalendarItem> Items { get; set; } = [];
}

public class FinnhubEarningsCalendarItem
{
    [JsonPropertyName("symbol")]         public string? Symbol { get; set; }
    [JsonPropertyName("date")]           public string? Date { get; set; }
    [JsonPropertyName("epsEstimate")]    public decimal? EpsEstimate { get; set; }
    [JsonPropertyName("revenueEstimate")] public decimal? RevenueEstimate { get; set; }
}

public class FinnhubSymbolSearch
{
    [JsonPropertyName("result")]
    public List<FinnhubSymbolItem> Result { get; set; } = [];
}

public class FinnhubSymbolItem
{
    [JsonPropertyName("symbol")]      public string? Symbol { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("type")]        public string? Type { get; set; }
    [JsonPropertyName("displaySymbol")] public string? DisplaySymbol { get; set; }
}

public class FinnhubCryptoSymbol
{
    [JsonPropertyName("symbol")]        public string? Symbol { get; set; }
    [JsonPropertyName("displaySymbol")] public string? DisplaySymbol { get; set; }
    [JsonPropertyName("description")]   public string? Description { get; set; }
}
