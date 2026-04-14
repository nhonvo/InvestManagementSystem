using InventoryAlert.Domain.DTOs;

namespace InventoryAlert.UnitTests.Helpers;

public static class PaginatedListFixtures
{
    public static PagedResult<T> Build<T>(IEnumerable<T> items, int total = 1, int page = 1)
    {
        return new PagedResult<T>
        {
            Items = items,
            TotalItems = total,
            PageNumber = page,
            PageSize = 10
        };
    }

    public static PagedResult<T> BuildEmpty<T>() => Build(Enumerable.Empty<T>(), 0);
}
