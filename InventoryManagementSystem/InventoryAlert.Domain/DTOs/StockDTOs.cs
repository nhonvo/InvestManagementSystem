namespace InventoryAlert.Domain.DTOs;

public record StockQuoteResponse(
    string Symbol,
    decimal Price,
    decimal Change,
    double ChangePercent,
    decimal High,
    decimal Low,
    decimal Open,
    decimal PrevClose,
    DateTime Timestamp);

public record StockProfileResponse(
    string Symbol,
    string Name,
    string? Exchange,
    string? Currency,
    string? Country,
    string? Industry,
    decimal? MarketCap,
    DateOnly? Ipo,
    string? WebUrl,
    string? Logo);

public record SymbolSearchResponse(
    string Symbol,
    string Description,
    string Type,
    string Exchange);
