namespace InventoryAlert.Api.Application.DTOs;

public record StockQuoteResponse(
    string Symbol,
    decimal Price,
    decimal Change,
    decimal ChangePercent,
    decimal High,
    decimal Low,
    decimal Open,
    decimal PrevClose,
    long Timestamp);

public record CompanyProfileResponse(
    string Symbol,
    string Name,
    string? Logo,
    string? Industry,
    string? Exchange,
    decimal? MarketCap,
    DateOnly? IpoDate,
    string? WebUrl,
    string? Country,
    string? Currency);

public record CompanyNewsResponse(
    string Headline,
    string? Summary,
    string? Source,
    string? Url,
    string? Image,
    string PublishedAt);

public record RecommendationResponse(
    string Period,
    int StrongBuy,
    int Buy,
    int Hold,
    int Sell,
    int StrongSell);

public record EarningsResponse(
    string Period,
    decimal? Actual,
    decimal? Estimate,
    decimal? Surprise,
    decimal? SurprisePercent);

public record MarketNewsResponse(
    string Headline,
    string? Summary,
    string? Source,
    string? Url,
    string? Image,
    string? Category,
    string PublishedAt);

public record MarketStatusResponse(
    string Exchange,
    bool IsOpen,
    string? Session,
    string? Holiday);

public record HolidayResponse(
    string AtDate,
    string EventName,
    string? TradingHour);

public record EarningsCalendarResponse(
    string Symbol,
    string Date,
    decimal? EpsEstimate,
    decimal? RevenueEstimate);

public record SymbolSearchResponse(
    string Symbol,
    string Description,
    string Type,
    string? Exchange);

public record CryptoExchangeResponse(string Exchange);

public record CryptoSymbolResponse(
    string Symbol,
    string DisplaySymbol,
    string Description);
