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
        Assert.True(balance.HasAverageCost);
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

    [Fact]
    public void NegativeQuantityAndValueToPositiveQuantityWithNegativeValueDoesNotEstablishNegativeAverage()
    {
        var balance = CreateKnownBalance(10, 10);
        balance.IssueSale(20, 200, DateTimeOffset.UtcNow);

        balance.ReceivePurchase(11, 11, DateTimeOffset.UtcNow);

        Assert.Equal(1, balance.QuantityOnHand);
        Assert.Equal(-89, balance.InventoryValue);
        Assert.Equal(10, balance.AverageCost);
        Assert.False(balance.HasAverageCost);
        Assert.Equal(CostReliability.Estimated, balance.ResolveSaleCost(1).Reliability);
    }

    [Fact]
    public void PositiveQuantityAndZeroValueEstablishesReliableZeroAverage()
    {
        var balance = CreateKnownBalance(10, 10);
        balance.IssueSale(20, 100, DateTimeOffset.UtcNow);

        balance.ReceivePurchase(11, 0, DateTimeOffset.UtcNow);

        Assert.Equal(1, balance.QuantityOnHand);
        Assert.Equal(0, balance.InventoryValue);
        Assert.Equal(0, balance.AverageCost);
        Assert.True(balance.HasAverageCost);
        Assert.Equal(CostReliability.Reliable, balance.ResolveSaleCost(123).Reliability);
    }

    [Fact]
    public void LaterPurchaseCanReestablishAverageAfterNegativeResidualValue()
    {
        var balance = CreateKnownBalance(10, 10);
        balance.IssueSale(20, 200, DateTimeOffset.UtcNow);
        balance.ReceivePurchase(11, 11, DateTimeOffset.UtcNow);

        balance.ReceivePurchase(9, 189, DateTimeOffset.UtcNow);

        Assert.Equal(10, balance.QuantityOnHand);
        Assert.Equal(100, balance.InventoryValue);
        Assert.Equal(10, balance.AverageCost);
        Assert.True(balance.HasAverageCost);
    }

    [Fact]
    public void ReturnAndVoidInboundUsesSameNegativeResidualGuardAsPurchase()
    {
        var balance = CreateKnownBalance(10, 10);
        balance.IssueSale(20, 200, DateTimeOffset.UtcNow);

        balance.ReceiveInbound(11, 11, DateTimeOffset.UtcNow);

        Assert.Equal(1, balance.QuantityOnHand);
        Assert.Equal(-89, balance.InventoryValue);
        Assert.Equal(10, balance.AverageCost);
        Assert.False(balance.HasAverageCost);
    }

    [Theory]
    [InlineData(5, 50, 0, 0)]
    [InlineData(10, 100, -5, -50)]
    public void PurchaseAtNonPositiveQuantityPreservesAverageMetadata(
        decimal soldQuantity,
        decimal soldValue,
        decimal expectedQuantity,
        decimal expectedValue)
    {
        var balance = CreateKnownBalance(10, 10);
        balance.IssueSale(10 + soldQuantity, 100 + soldValue, DateTimeOffset.UtcNow);

        balance.ReceivePurchase(5, 50, DateTimeOffset.UtcNow);

        Assert.Equal(expectedQuantity, balance.QuantityOnHand);
        Assert.Equal(expectedValue, balance.InventoryValue);
        Assert.Equal(10, balance.AverageCost);
        Assert.True(balance.HasAverageCost);
    }

    private static InventoryBalance CreateKnownBalance(decimal quantity, decimal cost) =>
        InventoryBalance.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            OpeningInventory.Create(quantity, cost),
            DateTimeOffset.UtcNow);
}
