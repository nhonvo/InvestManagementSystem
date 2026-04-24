using FluentAssertions;
using InventoryAlert.Domain.DTOs;

namespace InventoryAlert.IntegrationTests.TestUtils.Assertions;

public static class AlertRuleAssertion
{
    public static void AssertAllFieldsNotNull(AlertRuleResponse alertRule)
    {
        alertRule.Id.Should().NotBe(Guid.Empty);
        alertRule.TickerSymbol.Should().NotBe(null);
        alertRule.Condition.Should().NotBe(null);
        alertRule.TargetValue.Should().NotBe(null);
        //alertRule.IsActive.Should().NotBe();
    }
}
