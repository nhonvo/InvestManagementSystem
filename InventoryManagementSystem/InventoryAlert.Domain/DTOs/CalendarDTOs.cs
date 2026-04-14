namespace InventoryAlert.Domain.DTOs;

public record EarningsCalendarResponse(
    string Symbol,
    DateOnly Date,
    decimal? EpsEstimate,
    decimal? EpsActual,
    decimal? RevenueEstimate,
    decimal? RevenueActual);

public record IpoCalendarResponse(
    string Symbol,
    string Name,
    DateOnly Date,
    decimal? Price,
    long? Shares,
    string Status);
