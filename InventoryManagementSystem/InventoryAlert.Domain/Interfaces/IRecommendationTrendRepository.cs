using InventoryAlert.Domain.Entities.Postgres;

namespace InventoryAlert.Domain.Interfaces;

public interface IRecommendationTrendRepository
{
    Task<IEnumerable<RecommendationTrend>> GetBySymbolAsync(string symbol, CancellationToken ct);
    Task UpsertRangeAsync(IEnumerable<RecommendationTrend> recommendations, CancellationToken ct);
}
