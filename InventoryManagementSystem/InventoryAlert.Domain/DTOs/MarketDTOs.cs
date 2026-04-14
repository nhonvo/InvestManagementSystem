namespace InventoryAlert.Domain.DTOs;

public record StockMetricResponse(
    string Symbol,
    double? PeRatio,
    double? PbRatio,
    double? EpsBasicTtm,
    double? DividendYield,
    decimal? Week52High,
    decimal? Week52Low,
    double? RevenueGrowthTtm,
    double? MarginNet,
    DateTime LastSyncedAt);

public record EarningsSurpriseResponse(
    DateOnly Period,
    double? ActualEps,
    double? EstimateEps,
    double? SurprisePercent,
    DateOnly? ReportDate);

public record RecommendationResponse(
    string Period,
    int StrongBuy,
    int Buy,
    int Hold,
    int Sell,
    int StrongSell);

public record InsiderTransactionResponse(
    string? Name,
    long? Share,
    decimal? Value,
    DateOnly? TransactionDate,
    DateOnly? FilingDate,
    string? TransactionCode);

public record MarketStatusResponse(
    string Exchange,
    bool IsOpen,
    string? Session,
    string? Holiday,
    string? Timezone);

public record MarketHolidayResponse(
    string Exchange,
    string EventName,
    DateOnly Date,
    string? AtTime);

public record NewsResponse(
    long Id,
    string Headline,
    string Summary,
    string Source,
    string Url,
    DateTime DateTime,
    string? Image,
    string Category);

public record PeersResponse(
    string Symbol,
    List<string> Peers);
