using FluentAssertions;
using InventoryAlert.Domain.DTOs;

namespace InventoryAlert.IntegrationTests.TestUtils.Assertions;

public static class WatchlistItemAssertion
{
    public static void AssertAllFieldsNotNull(PortfolioPositionResponse item)
    {
        item.Symbol.Should().NotBe(null);
        item.StockId.Should().NotBe(null);
        item.Name.Should().NotBe(null);
        item.Exchange.Should().NotBe(null);
        item.Logo.Should().NotBe(null);
        item.HoldingsCount.Should().NotBe(null);
        item.AveragePrice.Should().NotBe(null);
        item.CurrentPrice.Should().NotBe(null);
        item.MarketValue.Should().NotBe(null);
        item.TotalCost.Should().NotBe(null);
        item.TotalReturn.Should().NotBe(null);
        item.TotalReturnPercent.Should().NotBe(null);
        item.PriceChange.Should().NotBe(null);
        item.PriceChangePercent.Should().NotBe(null);
        item.Industry.Should().NotBe(null);
    }
}
