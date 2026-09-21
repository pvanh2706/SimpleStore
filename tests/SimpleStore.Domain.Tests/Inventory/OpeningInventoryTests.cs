using SimpleStore.Domain;
using SimpleStore.Domain.Inventory;
using Xunit;

namespace SimpleStore.Domain.Tests.Inventory;

public sealed class OpeningInventoryTests
{
    [Fact]
    public void PositiveQuantityRequiresOpeningCost()
    {
        var exception = Assert.Throws<DomainRuleException>(() =>
            OpeningInventory.Create(20, null));

        Assert.Equal("opening-cost-required", exception.Code);
    }

    [Fact]
    public void OpeningInventoryCalculatesValueAndAverageCost()
    {
        var openingInventory = OpeningInventory.Create(20, 8_000);

        Assert.Equal(20, openingInventory.Quantity);
        Assert.Equal(160_000, openingInventory.InventoryValue);
        Assert.Equal(8_000, openingInventory.AverageCost);
    }

    [Fact]
    public void ZeroQuantityProducesZeroBalanceWithoutMovementRequirement()
    {
        var openingInventory = OpeningInventory.Create(0, null);

        Assert.False(openingInventory.HasStock);
        Assert.Equal(0, openingInventory.InventoryValue);
        Assert.Equal(0, openingInventory.AverageCost);
    }
}
