namespace InventoryAlert.Api.Application.DTOs;

public record WatchlistItemResponse(
    string Symbol,
    string Name,
    string Exchange,
    string Type,
    decimal? CurrentPrice,
    decimal? Change,
    decimal? ChangePercent,
    DateTime AddedAt);

public record AddToWatchlistRequest(string Symbol);
