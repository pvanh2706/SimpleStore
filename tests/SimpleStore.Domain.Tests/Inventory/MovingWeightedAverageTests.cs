using SimpleStore.Domain.Inventory;
using Xunit;

namespace SimpleStore.Domain.Tests.Inventory;

public sealed class MovingWeightedAverageTests
{
    [Fact]
    public void PurchaseReceiptUpdatesQuantityValueAndWeightedAverage()
    {
        var balance = InventoryBalance.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            OpeningInventory.Create(10, 10_000),
            DateTimeOffset.UtcNow);

        balance.ReceivePurchase(20, 260_000, DateTimeOffset.UtcNow);

        Assert.Equal(30, balance.QuantityOnHand);
        Assert.Equal(360_000, balance.InventoryValue);
        Assert.Equal(12_000, balance.AverageCost);
    }

    [Fact]
    public void WeightedAverageUsesAwayFromZeroAtMidpoint()
    {
        var balance = InventoryBalance.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            OpeningInventory.Create(1, 1),
            DateTimeOffset.UtcNow);

        balance.ReceivePurchase(1, 1.0001m, DateTimeOffset.UtcNow);

        Assert.Equal(1.0001m, balance.AverageCost);
    }
}
