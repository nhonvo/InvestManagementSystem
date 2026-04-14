using InventoryAlert.Domain.Entities.Postgres;

namespace InventoryAlert.Domain.DTOs;

public record CreatePositionRequest(
    string TickerSymbol,
    decimal Quantity,
    decimal UnitPrice,
    DateTime? TradedAt);

public record TradeRequest(
    TradeType Type,
    decimal Quantity,
    decimal UnitPrice,
    string? Notes);

public record PortfolioPositionResponse(
    int StockId,
    string Symbol,
    string Name,
    string? Exchange,
    string? Logo,
    decimal HoldingsCount,
    decimal AveragePrice,
    decimal CurrentPrice,
    decimal MarketValue,
    decimal TotalCost,
    decimal TotalReturn,
    double TotalReturnPercent,
    decimal PriceChange,
    decimal PriceChangePercent,
    string? Industry);

public record PortfolioAlertResponse(
    string Symbol,
    decimal CurrentPrice,
    decimal Threshold,
    double LossPercent,
    DateTime LastUpdated);

public class PortfolioQueryParams : PaginationParams
{
    public string? Search { get; set; }
}
